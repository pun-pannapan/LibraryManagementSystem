using Asp.Versioning;
using FluentValidation;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Infrastructure.Persistence;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Domain.Enums;
using LibraryManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Endpoints;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/books")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Books");

        group.MapGet("/", GetBooksAsync)
            .RequireAuthorization()
            .Produces<PagedResult<BookDto>>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetBookByIdAsync)
            .RequireAuthorization()
            .Produces<BookDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateBookAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.Administrator))
            .Produces<BookDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateBookAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.Administrator))
            .Produces<BookDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteBookAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.Administrator))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetBooksAsync(
        ApplicationDbContext dbContext,
        IValidator<GetBooksQuery> validator,
        [FromQuery] string? search,
        [FromQuery] string? title,
        [FromQuery] string? author,
        [FromQuery] string? isbn,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? available,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var request = new GetBooksQuery(search, title, author, isbn, categoryId, available, sortBy, sortDirection, page, pageSize);
        var validation = EndpointHelpers.ValidationProblem(await validator.ValidateAsync(request, cancellationToken));
        if (validation is not null)
        {
            return validation;
        }

        var query = dbContext.Books
            .AsNoTracking()
            .Include(book => book.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(book =>
                book.Title.Contains(value)
                || book.Author.Contains(value)
                || book.Isbn.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            query = query.Where(book => book.Title.Contains(title.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            query = query.Where(book => book.Author.Contains(author.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(isbn))
        {
            query = query.Where(book => book.Isbn.Contains(isbn.Trim()));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(book => book.CategoryId == categoryId.Value);
        }

        if (available.HasValue)
        {
            query = query.Where(book => book.AvailabilityStatus ==
                (available.Value ? BookAvailabilityStatus.Available : BookAvailabilityStatus.Borrowed));
        }

        query = ApplySorting(query, sortBy, sortDirection);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(book => new BookDto(
                book.Id,
                book.Isbn,
                book.Title,
                book.Author,
                book.Publisher,
                book.PublishedYear,
                book.CategoryId,
                book.Category.Name,
                book.AvailabilityStatus,
                Convert.ToBase64String(book.RowVersion)))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<BookDto>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)));
    }

    private static async Task<IResult> GetBookByIdAsync(
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var book = await dbContext.Books
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return book is null ? Results.NotFound() : Results.Ok(EndpointHelpers.ToDto(book));
    }

    private static async Task<IResult> CreateBookAsync(
        CreateBookRequest request,
        ApplicationDbContext dbContext,
        IValidator<CreateBookRequest> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Books");

        var validation = EndpointHelpers.ValidationProblem(await validator.ValidateAsync(request, cancellationToken))
            ?? await ValidateBookBusinessRulesAsync(request.Isbn, request.CategoryId, dbContext, null, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var book = new Book
        {
            Isbn = request.Isbn.Trim(),
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Publisher = NormalizeOptional(request.Publisher),
            PublishedYear = request.PublishedYear,
            CategoryId = request.CategoryId
        };

        dbContext.Books.Add(book);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EndpointHelpers.IsDuplicateKey(exception))
        {
            logger.LogInformation("Book creation conflict because ISBN already exists");
            return Results.Conflict(new { message = "ISBN must be unique." });
        }

        await dbContext.Entry(book).Reference(item => item.Category).LoadAsync(cancellationToken);
        logger.LogInformation("Book {BookId} created", book.Id);
        return Results.Created($"/api/v1/books/{book.Id}", EndpointHelpers.ToDto(book));
    }

    private static async Task<IResult> UpdateBookAsync(
        Guid id,
        UpdateBookRequest request,
        ApplicationDbContext dbContext,
        IValidator<UpdateBookRequest> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Books");

        var validation = EndpointHelpers.ValidationProblem(await validator.ValidateAsync(request, cancellationToken))
            ?? await ValidateBookBusinessRulesAsync(request.Isbn, request.CategoryId, dbContext, id, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var rowVersion = Convert.FromBase64String(request.RowVersion);
        var book = await dbContext.Books
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (book is null)
        {
            logger.LogInformation("Book update failed because book {BookId} was not found", id);
            return Results.NotFound();
        }

        book.Isbn = request.Isbn.Trim();
        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.Publisher = NormalizeOptional(request.Publisher);
        book.PublishedYear = request.PublishedYear;
        book.CategoryId = request.CategoryId;
        dbContext.Entry(book).Property(item => item.RowVersion).OriginalValue = rowVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Book update conflict for book {BookId}", id);
            return Results.Conflict(new { message = "The book was modified by another request. Refresh and try again." });
        }
        catch (DbUpdateException exception) when (EndpointHelpers.IsDuplicateKey(exception))
        {
            logger.LogInformation("Book update conflict for book {BookId} because ISBN already exists", id);
            return Results.Conflict(new { message = "ISBN must be unique." });
        }

        await dbContext.Entry(book).Reference(item => item.Category).LoadAsync(cancellationToken);
        logger.LogInformation("Book {BookId} updated", book.Id);
        return Results.Ok(EndpointHelpers.ToDto(book));
    }

    private static async Task<IResult> DeleteBookAsync(
        Guid id,
        ApplicationDbContext dbContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Books");

        var book = await dbContext.Books.FindAsync([id], cancellationToken);
        if (book is null)
        {
            logger.LogInformation("Book delete failed because book {BookId} was not found", id);
            return Results.NotFound();
        }

        var hasHistory = await dbContext.BorrowTransactions
            .AnyAsync(transaction => transaction.BookId == id, cancellationToken);

        if (hasHistory)
        {
            logger.LogInformation("Book delete conflict for book {BookId} because borrowing history exists", id);
            return Results.Conflict(new { message = "Books with borrowing history cannot be deleted." });
        }

        dbContext.Books.Remove(book);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Book {BookId} deleted", id);
        return Results.NoContent();
    }

    private static IQueryable<Book> ApplySorting(
        IQueryable<Book> query,
        string? sortBy,
        string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "author" => descending
                ? query.OrderByDescending(book => book.Author).ThenBy(book => book.Title)
                : query.OrderBy(book => book.Author).ThenBy(book => book.Title),
            "publicationyear" or "publishedyear" => descending
                ? query.OrderByDescending(book => book.PublishedYear).ThenBy(book => book.Title)
                : query.OrderBy(book => book.PublishedYear).ThenBy(book => book.Title),
            _ => descending
                ? query.OrderByDescending(book => book.Title)
                : query.OrderBy(book => book.Title)
        };
    }

    private static async Task<IResult?> ValidateBookBusinessRulesAsync(
        string isbn,
        Guid categoryId,
        ApplicationDbContext dbContext,
        Guid? currentBookId,
        CancellationToken cancellationToken)
    {
        var errors = new List<(string Field, string Error)>();

        if (!await dbContext.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken))
        {
            errors.Add((nameof(categoryId), "Category does not exist."));
        }

        if (!string.IsNullOrWhiteSpace(isbn))
        {
            var duplicateExists = await dbContext.Books.AnyAsync(
                book => book.Isbn == isbn.Trim() && (!currentBookId.HasValue || book.Id != currentBookId),
                cancellationToken);

            if (duplicateExists)
            {
                errors.Add((nameof(isbn), "ISBN must be unique."));
            }
        }

        return errors.Count == 0 ? null : EndpointHelpers.ValidationProblem(errors.ToArray());
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

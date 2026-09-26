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

public static class BorrowingEndpoints
{
    public static IEndpointRouteBuilder MapBorrowingEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/borrowings")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization()
            .WithTags("Borrowings");

        group.MapPost("/", BorrowBookAsync)
            .Produces<BorrowTransactionDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/return", ReturnBookAsync)
            .Produces<BorrowTransactionDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/me", GetMyHistoryAsync)
            .Produces<PagedResult<BorrowTransactionDto>>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/", GetAllHistoryAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.Administrator))
            .Produces<PagedResult<BorrowTransactionDto>>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> BorrowBookAsync(
        BorrowBookRequest request,
        HttpContext context,
        ApplicationDbContext dbContext,
        IValidator<BorrowBookRequest> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Borrowings");

        var validation = EndpointHelpers.ValidationProblem(await validator.ValidateAsync(request, cancellationToken));
        if (validation is not null)
        {
            return validation;
        }

        if (!EndpointHelpers.TryGetUserId(context.User, out var userId))
        {
            return Results.Unauthorized();
        }

        logger.LogInformation("Borrow attempt for book {BookId} by user {UserId}", request.BookId, userId);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var book = await dbContext.Books
                .FirstOrDefaultAsync(item => item.Id == request.BookId, cancellationToken);

            if (book is null)
            {
                logger.LogInformation("Borrow failed because book {BookId} was not found", request.BookId);
                return Results.NotFound();
            }

            if (book.AvailabilityStatus != BookAvailabilityStatus.Available)
            {
                logger.LogInformation("Borrow conflict for book {BookId} because status is {Status}", request.BookId, book.AvailabilityStatus);
                return Results.Conflict(new { message = "The selected book is already borrowed." });
            }

            var hasActiveBorrowing = await dbContext.BorrowTransactions.AnyAsync(
                item => item.BookId == request.BookId && item.Status == BorrowTransactionStatus.Borrowed,
                cancellationToken);

            if (hasActiveBorrowing)
            {
                logger.LogInformation("Borrow conflict for book {BookId} because an active transaction exists", request.BookId);
                return Results.Conflict(new { message = "The selected book already has an active borrowing transaction." });
            }

            var now = DateTime.UtcNow;
            var borrowing = new BorrowTransaction
            {
                BookId = request.BookId,
                UserId = userId,
                BorrowedAtUtc = now,
                DueAtUtc = now.AddDays(14),
                Status = BorrowTransactionStatus.Borrowed
            };

            book.AvailabilityStatus = BookAvailabilityStatus.Borrowed;
            dbContext.BorrowTransactions.Add(borrowing);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogInformation("Concurrency conflict while borrowing book {BookId}", request.BookId);
                return Results.Conflict(new { message = "The selected book was borrowed by another request." });
            }

            var created = await LoadTransactionAsync(dbContext, borrowing.Id, cancellationToken);
            var borrowerEmail = await GetUserEmailAsync(dbContext, borrowing.UserId, cancellationToken);
            logger.LogInformation("Borrowing {BorrowingId} created for book {BookId} by user {UserId}", borrowing.Id, borrowing.BookId, borrowing.UserId);
            return Results.Created($"/api/v1/borrowings/{borrowing.Id}", EndpointHelpers.ToDto(created!, borrowerEmail));
        });
    }

    private static async Task<IResult> ReturnBookAsync(
        Guid id,
        HttpContext context,
        ApplicationDbContext dbContext,
        IValidator<ReturnBorrowingRequest> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Borrowings");

        var validation = EndpointHelpers.ValidationProblem(
            await validator.ValidateAsync(new ReturnBorrowingRequest(id), cancellationToken));
        if (validation is not null)
        {
            return validation;
        }

        if (!EndpointHelpers.TryGetUserId(context.User, out var userId))
        {
            return Results.Unauthorized();
        }

        var isAdmin = context.User.IsInRole(ApplicationRoles.Administrator);
        logger.LogInformation("Return attempt for borrowing {BorrowingId} by user {UserId}", id, userId);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var borrowing = await dbContext.BorrowTransactions
                .Include(item => item.Book)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (borrowing is null)
            {
                logger.LogInformation("Return failed because borrowing {BorrowingId} was not found", id);
                return Results.NotFound();
            }

            if (!isAdmin && borrowing.UserId != userId)
            {
                logger.LogInformation("Return forbidden for borrowing {BorrowingId} by user {UserId}", id, userId);
                return Results.Forbid();
            }

            if (borrowing.Status == BorrowTransactionStatus.Returned)
            {
                logger.LogInformation("Return conflict for borrowing {BorrowingId} because it is already returned", id);
                return Results.Conflict(new { message = "This borrowing has already been returned." });
            }

            borrowing.ReturnedAtUtc = DateTime.UtcNow;
            borrowing.Status = BorrowTransactionStatus.Returned;
            borrowing.Book.AvailabilityStatus = BookAvailabilityStatus.Available;

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogInformation("Concurrency conflict while returning borrowing {BorrowingId}", id);
                return Results.Conflict(new { message = "This borrowing was modified by another request." });
            }

            var borrowerEmail = await GetUserEmailAsync(dbContext, borrowing.UserId, cancellationToken);
            logger.LogInformation("Borrowing {BorrowingId} returned for book {BookId}", borrowing.Id, borrowing.BookId);
            return Results.Ok(EndpointHelpers.ToDto(borrowing, borrowerEmail));
        });
    }

    private static async Task<IResult> GetMyHistoryAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        IValidator<BorrowingHistoryQuery> validator,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!EndpointHelpers.TryGetUserId(context.User, out var userId))
        {
            return Results.Unauthorized();
        }

        var query = dbContext.BorrowTransactions
            .AsNoTracking()
            .Include(item => item.Book)
            .Where(item => item.UserId == userId);

        return await GetHistoryAsync(dbContext, query, new BorrowingHistoryQuery(status, sort, search, page, pageSize), validator, cancellationToken);
    }

    private static async Task<IResult> GetAllHistoryAsync(
        ApplicationDbContext dbContext,
        IValidator<BorrowingHistoryQuery> validator,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? bookId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] DateTime? borrowedFrom,
        [FromQuery] DateTime? borrowedTo,
        [FromQuery] DateTime? returnedFrom,
        [FromQuery] DateTime? returnedTo,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.BorrowTransactions
            .AsNoTracking()
            .Include(item => item.Book)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(item => item.UserId == userId.Value);
        }

        if (bookId.HasValue)
        {
            query = query.Where(item => item.BookId == bookId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(item =>
                item.Book.Title.Contains(value)
                || item.Book.Isbn.Contains(value)
                || dbContext.Users.Any(user => user.Id == item.UserId
                    && ((user.Email != null && user.Email.Contains(value))
                        || (user.UserName != null && user.UserName.Contains(value))
                        || (user.FirstName != null && user.FirstName.Contains(value))
                        || (user.LastName != null && user.LastName.Contains(value)))));
        }

        if (borrowedFrom.HasValue)
        {
            query = query.Where(item => item.BorrowedAtUtc >= borrowedFrom.Value);
        }

        if (borrowedTo.HasValue)
        {
            query = query.Where(item => item.BorrowedAtUtc <= borrowedTo.Value);
        }

        if (returnedFrom.HasValue)
        {
            query = query.Where(item => item.ReturnedAtUtc >= returnedFrom.Value);
        }

        if (returnedTo.HasValue)
        {
            query = query.Where(item => item.ReturnedAtUtc <= returnedTo.Value);
        }

        return await GetHistoryAsync(dbContext, query, new BorrowingHistoryQuery(status, sort, search, page, pageSize), validator, cancellationToken);
    }

    private static async Task<IResult> GetHistoryAsync(
        ApplicationDbContext dbContext,
        IQueryable<BorrowTransaction> query,
        BorrowingHistoryQuery request,
        IValidator<BorrowingHistoryQuery> validator,
        CancellationToken cancellationToken)
    {
        var validation = EndpointHelpers.ValidationProblem(await validator.ValidateAsync(request, cancellationToken));
        if (validation is not null)
        {
            return validation;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            Enum.TryParse<BorrowTransactionStatus>(request.Status, ignoreCase: true, out var parsedStatus);
            query = query.Where(item => item.Status == parsedStatus);
        }

        query = ApplyHistorySorting(query, NormalizeHistorySort(request.Sort));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new BorrowTransactionDto(
                item.Id,
                item.BookId,
                item.Book.Title,
                item.UserId,
                dbContext.Users
                    .Where(user => user.Id == item.UserId)
                    .Select(user => user.Email ?? user.UserName ?? string.Empty)
                    .FirstOrDefault() ?? string.Empty,
                item.BorrowedAtUtc,
                item.DueAtUtc,
                item.ReturnedAtUtc,
                item.Status))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<BorrowTransactionDto>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }

    private static string NormalizeHistorySort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return "-borrowedat";
        }

        var normalized = sort.Trim().ToLowerInvariant();
        return normalized.TrimStart('-') is "borrowedat" or "dueat" or "returnedat" or "status"
            ? normalized
            : "-borrowedat";
    }

    private static IQueryable<BorrowTransaction> ApplyHistorySorting(
        IQueryable<BorrowTransaction> query,
        string sort)
    {
        var descending = sort.StartsWith('-');
        var field = sort.TrimStart('-');

        return field switch
        {
            "dueat" => descending
                ? query.OrderByDescending(item => item.DueAtUtc)
                : query.OrderBy(item => item.DueAtUtc),
            "returnedat" => descending
                ? query.OrderByDescending(item => item.ReturnedAtUtc)
                : query.OrderBy(item => item.ReturnedAtUtc),
            "status" => descending
                ? query.OrderByDescending(item => item.Status)
                : query.OrderBy(item => item.Status),
            _ => descending
                ? query.OrderByDescending(item => item.BorrowedAtUtc)
                : query.OrderBy(item => item.BorrowedAtUtc)
        };
    }

    private static Task<BorrowTransaction?> LoadTransactionAsync(
        ApplicationDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.BorrowTransactions
            .AsNoTracking()
            .Include(item => item.Book)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    private static async Task<string> GetUserEmailAsync(
        ApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => user.Email ?? user.UserName ?? string.Empty)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
}

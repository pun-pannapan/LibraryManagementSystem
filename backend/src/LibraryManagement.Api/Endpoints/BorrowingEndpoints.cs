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
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.User))
            .Produces<BorrowTransactionDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/return", RequestReturnAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.User))
            .Produces<BorrowTransactionDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/request-return", RequestReturnAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.User))
            .Produces<BorrowTransactionDto>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/cancel", CancelBorrowRequestAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.User))
            .Produces<BorrowTransactionDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/assign", AssignBorrowingAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.Administrator))
            .Produces<BorrowTransactionDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reject", RejectBorrowingAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.Administrator))
            .Produces<BorrowTransactionDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/accept-return", AcceptReturnAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.Administrator))
            .Produces<BorrowTransactionDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/me", GetMyHistoryAsync)
            .RequireAuthorization(policy => policy.RequireRole(ApplicationRoles.User))
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
                return Results.Conflict(new { message = "The selected book is not available for a borrow request." });
            }

            var hasActiveBorrowing = await dbContext.BorrowTransactions.AnyAsync(
                item => item.BookId == request.BookId
                    && (item.Status == BorrowTransactionStatus.Requested
                        || item.Status == BorrowTransactionStatus.Borrowed
                        || item.Status == BorrowTransactionStatus.ReturnRequested),
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
                RequestedAtUtc = now,
                Status = BorrowTransactionStatus.Requested
            };

            book.AvailabilityStatus = BookAvailabilityStatus.Reserved;
            dbContext.BorrowTransactions.Add(borrowing);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogInformation("Concurrency conflict while borrowing book {BookId}", request.BookId);
                return Results.Conflict(new { message = "The selected book was requested by another user." });
            }

            var created = await LoadTransactionAsync(dbContext, borrowing.Id, cancellationToken);
            var borrowerEmail = await GetUserEmailAsync(dbContext, borrowing.UserId, cancellationToken);
            logger.LogInformation("Borrow request {BorrowingId} created for book {BookId} by user {UserId}", borrowing.Id, borrowing.BookId, borrowing.UserId);
            return Results.Created($"/api/v1/borrowings/{borrowing.Id}", EndpointHelpers.ToDto(created!, borrowerEmail));
        });
    }

    private static async Task<IResult> RequestReturnAsync(
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

            if (borrowing.UserId != userId)
            {
                logger.LogInformation("Return forbidden for borrowing {BorrowingId} by user {UserId}", id, userId);
                return Results.Forbid();
            }

            if (borrowing.Status != BorrowTransactionStatus.Borrowed)
            {
                logger.LogInformation("Return conflict for borrowing {BorrowingId} because status is {Status}", id, borrowing.Status);
                return Results.Conflict(new { message = "Only an active borrowed item can request a return." });
            }

            borrowing.ReturnRequestedAtUtc = DateTime.UtcNow;
            borrowing.Status = BorrowTransactionStatus.ReturnRequested;

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
            logger.LogInformation("Return requested for borrowing {BorrowingId} and book {BookId}", borrowing.Id, borrowing.BookId);
            return Results.Ok(EndpointHelpers.ToDto(borrowing, borrowerEmail));
        });
    }

    private static async Task<IResult> CancelBorrowRequestAsync(
        Guid id,
        HttpContext context,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!EndpointHelpers.TryGetUserId(context.User, out var userId))
        {
            return Results.Unauthorized();
        }

        var borrowing = await dbContext.BorrowTransactions
            .Include(item => item.Book)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (borrowing is null) return Results.NotFound();
        if (borrowing.UserId != userId) return Results.Forbid();
        if (borrowing.Status != BorrowTransactionStatus.Requested)
            return Results.Conflict(new { message = "Only a pending borrow request can be cancelled." });

        borrowing.Status = BorrowTransactionStatus.Cancelled;
        borrowing.CancelledAtUtc = DateTime.UtcNow;
        borrowing.Book.AvailabilityStatus = BookAvailabilityStatus.Available;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "This borrow request was modified by another request." });
        }
        return Results.Ok(EndpointHelpers.ToDto(
            borrowing,
            await GetUserEmailAsync(dbContext, borrowing.UserId, cancellationToken)));
    }

    private static async Task<IResult> AssignBorrowingAsync(
        Guid id,
        HttpContext context,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!EndpointHelpers.TryGetUserId(context.User, out var administratorId))
        {
            return Results.Unauthorized();
        }

        var borrowing = await dbContext.BorrowTransactions
            .Include(item => item.Book)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (borrowing is null) return Results.NotFound();
        if (borrowing.Status != BorrowTransactionStatus.Requested
            || borrowing.Book.AvailabilityStatus != BookAvailabilityStatus.Reserved)
            return Results.Conflict(new { message = "Only a pending request can be assigned." });

        var now = DateTime.UtcNow;
        borrowing.Status = BorrowTransactionStatus.Borrowed;
        borrowing.BorrowedAtUtc = now;
        borrowing.AssignedAtUtc = now;
        borrowing.AssignedByUserId = administratorId;
        borrowing.DueAtUtc = now.AddDays(14);
        borrowing.Book.AvailabilityStatus = BookAvailabilityStatus.Borrowed;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "This borrow request was modified by another administrator." });
        }
        return Results.Ok(EndpointHelpers.ToDto(
            borrowing,
            await GetUserEmailAsync(dbContext, borrowing.UserId, cancellationToken)));
    }

    private static async Task<IResult> RejectBorrowingAsync(
        Guid id,
        HttpContext context,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!EndpointHelpers.TryGetUserId(context.User, out var administratorId))
        {
            return Results.Unauthorized();
        }

        var borrowing = await dbContext.BorrowTransactions
            .Include(item => item.Book)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (borrowing is null) return Results.NotFound();
        if (borrowing.Status != BorrowTransactionStatus.Requested)
            return Results.Conflict(new { message = "Only a pending request can be rejected." });

        borrowing.Status = BorrowTransactionStatus.Rejected;
        borrowing.RejectedAtUtc = DateTime.UtcNow;
        borrowing.RejectedByUserId = administratorId;
        borrowing.Book.AvailabilityStatus = BookAvailabilityStatus.Available;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "This borrow request was modified by another administrator." });
        }
        return Results.Ok(EndpointHelpers.ToDto(
            borrowing,
            await GetUserEmailAsync(dbContext, borrowing.UserId, cancellationToken)));
    }

    private static async Task<IResult> AcceptReturnAsync(
        Guid id,
        HttpContext context,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!EndpointHelpers.TryGetUserId(context.User, out var administratorId))
        {
            return Results.Unauthorized();
        }

        var borrowing = await dbContext.BorrowTransactions
            .Include(item => item.Book)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (borrowing is null) return Results.NotFound();
        if (borrowing.Status != BorrowTransactionStatus.ReturnRequested)
            return Results.Conflict(new { message = "Only a pending return can be accepted." });

        borrowing.Status = BorrowTransactionStatus.Returned;
        borrowing.ReturnedAtUtc = DateTime.UtcNow;
        borrowing.ProcessedByUserId = administratorId;
        borrowing.Book.AvailabilityStatus = BookAvailabilityStatus.Available;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "This return request was modified by another administrator." });
        }
        return Results.Ok(EndpointHelpers.ToDto(
            borrowing,
            await GetUserEmailAsync(dbContext, borrowing.UserId, cancellationToken)));
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
                item.Status,
                item.RequestedAtUtc,
                item.AssignedAtUtc,
                item.ReturnRequestedAtUtc,
                item.AssignedByUserId,
                item.ProcessedByUserId,
                item.RejectedAtUtc,
                item.RejectedByUserId,
                item.CancelledAtUtc))
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
            return $"-{BorrowingHistorySortFields.RequestedAt}";
        }

        var normalized = sort.Trim().ToLowerInvariant();
        return BorrowingHistorySortFields.All.Contains(normalized.TrimStart('-'))
            ? normalized
            : $"-{BorrowingHistorySortFields.RequestedAt}";
    }

    private static IQueryable<BorrowTransaction> ApplyHistorySorting(
        IQueryable<BorrowTransaction> query,
        string sort)
    {
        var descending = sort.StartsWith('-');
        var field = sort.TrimStart('-');

        return field switch
        {
            BorrowingHistorySortFields.RequestedAt => descending
                ? query.OrderByDescending(item => item.RequestedAtUtc).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.RequestedAtUtc).ThenBy(item => item.Id),
            BorrowingHistorySortFields.AssignedAt => descending
                ? query.OrderByDescending(item => item.AssignedAtUtc).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.AssignedAtUtc).ThenBy(item => item.Id),
            BorrowingHistorySortFields.ReturnRequestedAt => descending
                ? query.OrderByDescending(item => item.ReturnRequestedAtUtc).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.ReturnRequestedAtUtc).ThenBy(item => item.Id),
            BorrowingHistorySortFields.DueAt => descending
                ? query.OrderByDescending(item => item.DueAtUtc).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.DueAtUtc).ThenBy(item => item.Id),
            BorrowingHistorySortFields.ReturnedAt => descending
                ? query.OrderByDescending(item => item.ReturnedAtUtc).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.ReturnedAtUtc).ThenBy(item => item.Id),
            BorrowingHistorySortFields.Status => descending
                ? query.OrderByDescending(item => item.Status).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.Status).ThenBy(item => item.Id),
            _ => descending
                ? query.OrderByDescending(item => item.BorrowedAtUtc).ThenByDescending(item => item.Id)
                : query.OrderBy(item => item.BorrowedAtUtc).ThenBy(item => item.Id)
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

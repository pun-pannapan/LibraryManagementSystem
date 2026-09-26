using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Application.Contracts;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    UserDto User);

public sealed record UserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    IReadOnlyCollection<string> Roles);

public sealed record CategoryDto(Guid Id, string Name, string? Description);

public sealed record BookDto(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublishedYear,
    Guid CategoryId,
    string CategoryName,
    BookAvailabilityStatus AvailabilityStatus,
    string RowVersion);

public sealed record CreateBookRequest(
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublishedYear,
    Guid CategoryId);

public sealed record UpdateBookRequest(
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublishedYear,
    Guid CategoryId,
    string RowVersion);

public sealed record GetBooksQuery(
    string? Search,
    string? Title,
    string? Author,
    string? Isbn,
    Guid? CategoryId,
    bool? Available,
    string? SortBy,
    string? SortDirection,
    int Page,
    int PageSize);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record BorrowBookRequest(Guid BookId);

public sealed record ReturnBorrowingRequest(Guid BorrowingId);

public sealed record BorrowingHistoryQuery(
    string? Status,
    string? Sort,
    string? Search,
    int Page,
    int PageSize);

public sealed record BorrowTransactionDto(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid UserId,
    string UserEmail,
    DateTime BorrowedAtUtc,
    DateTime DueAtUtc,
    DateTime? ReturnedAtUtc,
    BorrowTransactionStatus Status);

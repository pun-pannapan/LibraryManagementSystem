using FluentValidation;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Domain.Enums;

namespace LibraryManagement.Application.Validation;

public sealed class BorrowBookRequestValidator : AbstractValidator<BorrowBookRequest>
{
    public BorrowBookRequestValidator()
    {
        RuleFor(request => request.BookId).NotEmpty();
    }
}

public sealed class ReturnBorrowingRequestValidator : AbstractValidator<ReturnBorrowingRequest>
{
    public ReturnBorrowingRequestValidator()
    {
        RuleFor(request => request.BorrowingId).NotEmpty();
    }
}

public sealed class BorrowingHistoryQueryValidator : AbstractValidator<BorrowingHistoryQuery>
{
    public BorrowingHistoryQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);

        RuleFor(query => query.Status)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || Enum.TryParse<BorrowTransactionStatus>(value, ignoreCase: true, out _))
            .WithMessage("Status must be Requested, Borrowed, ReturnRequested, Returned, Rejected, or Cancelled.");

        RuleFor(query => query.Search).MaximumLength(250);

        RuleFor(query => query.Sort)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || BorrowingHistorySortFields.All.Contains(value.Trim().TrimStart('-')))
            .WithMessage("Sort must be requestedAt, assignedAt, returnRequestedAt, borrowedAt, dueAt, returnedAt, status, or the same value prefixed with '-' for descending.");
    }
}

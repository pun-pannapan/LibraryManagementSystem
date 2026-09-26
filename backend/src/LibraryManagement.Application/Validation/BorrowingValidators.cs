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
    private static readonly string[] AllowedSortFields = ["borrowedat", "dueat", "returnedat", "status"];

    public BorrowingHistoryQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);

        RuleFor(query => query.Status)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || Enum.TryParse<BorrowTransactionStatus>(value, ignoreCase: true, out _))
            .WithMessage("Status must be Borrowed or Returned.");

        RuleFor(query => query.Search).MaximumLength(250);

        RuleFor(query => query.Sort)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || AllowedSortFields.Contains(value.Trim().TrimStart('-').ToLowerInvariant()))
            .WithMessage("Sort must be borrowedAt, dueAt, returnedAt, status, or the same value prefixed with '-' for descending.");
    }
}

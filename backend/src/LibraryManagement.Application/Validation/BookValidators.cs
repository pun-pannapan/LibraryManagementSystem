using FluentValidation;
using LibraryManagement.Application.Contracts;
using System.Linq.Expressions;

namespace LibraryManagement.Application.Validation;

public sealed class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        Include(new BookRequestRules<CreateBookRequest>(
            request => request.Isbn,
            request => request.Title,
            request => request.Author,
            request => request.Publisher,
            request => request.PublishedYear,
            request => request.ShelfCode,
            request => request.Location,
            request => request.CategoryId));
    }
}

public sealed class UpdateBookRequestValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookRequestValidator()
    {
        Include(new BookRequestRules<UpdateBookRequest>(
            request => request.Isbn,
            request => request.Title,
            request => request.Author,
            request => request.Publisher,
            request => request.PublishedYear,
            request => request.ShelfCode,
            request => request.Location,
            request => request.CategoryId));

        RuleFor(request => request.RowVersion)
            .NotEmpty()
            .Must(IsBase64)
            .WithMessage("A valid row version is required.");
    }

    private static bool IsBase64(string value) =>
        Convert.TryFromBase64String(value, new byte[16], out var bytesWritten) && bytesWritten > 0;
}

public sealed class GetBooksQueryValidator : AbstractValidator<GetBooksQuery>
{
    public GetBooksQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(250);
        RuleFor(query => query.Title).MaximumLength(250);
        RuleFor(query => query.Author).MaximumLength(200);
        RuleFor(query => query.Isbn).MaximumLength(20);
        RuleFor(query => query.ShelfCode).MaximumLength(50);
        RuleFor(query => query.Location).MaximumLength(150);
        RuleFor(query => query.CategoryId).NotEmpty().When(query => query.CategoryId.HasValue);
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);

        RuleFor(query => query.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || BookSortFields.All.Contains(value.Trim()))
            .WithMessage("Sort by must be title, author, isbn, category, publicationYear, shelfCode, or location.");

        RuleFor(query => query.SortDirection)
            .Must(value => string.IsNullOrWhiteSpace(value)
                || string.Equals(value.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value.Trim(), "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be asc or desc.");
    }
}

internal sealed class BookRequestRules<T> : AbstractValidator<T>
{
    public BookRequestRules(
        Expression<Func<T, string>> isbn,
        Expression<Func<T, string>> title,
        Expression<Func<T, string>> author,
        Expression<Func<T, string?>> publisher,
        Expression<Func<T, int?>> publishedYear,
        Expression<Func<T, string?>> shelfCode,
        Expression<Func<T, string?>> location,
        Expression<Func<T, Guid>> categoryId)
    {
        var publishedYearAccessor = publishedYear.Compile();

        RuleFor(isbn)
            .NotEmpty()
            .MaximumLength(20)
            .WithName(nameof(CreateBookRequest.Isbn));

        RuleFor(title)
            .NotEmpty()
            .MaximumLength(250)
            .WithName(nameof(CreateBookRequest.Title));

        RuleFor(author)
            .NotEmpty()
            .MaximumLength(200)
            .WithName(nameof(CreateBookRequest.Author));

        RuleFor(publisher)
            .MaximumLength(200)
            .WithName(nameof(CreateBookRequest.Publisher));

        RuleFor(publishedYear)
            .InclusiveBetween(1000, DateTime.UtcNow.Year + 1)
            .When(request => publishedYearAccessor(request).HasValue)
            .WithName(nameof(CreateBookRequest.PublishedYear));

        RuleFor(shelfCode).MaximumLength(50).WithName(nameof(CreateBookRequest.ShelfCode));
        RuleFor(location).MaximumLength(150).WithName(nameof(CreateBookRequest.Location));

        RuleFor(categoryId)
            .NotEmpty()
            .WithName(nameof(CreateBookRequest.CategoryId));
    }
}

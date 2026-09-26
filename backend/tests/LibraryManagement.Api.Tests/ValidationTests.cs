using FluentAssertions;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Application.Validation;

namespace LibraryManagement.Api.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void RegisterRequestValidator_RejectsInvalidEmailAndShortPassword()
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest("not-an-email", "short", null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Email));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void CreateBookRequestValidator_RejectsMissingTitleAndInvalidCategory()
    {
        var validator = new CreateBookRequestValidator();

        var result = validator.Validate(new CreateBookRequest(
            "9780132350884",
            "",
            "Robert C. Martin",
            "Prentice Hall",
            2008,
            Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateBookRequest.Title));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateBookRequest.CategoryId));
    }

    [Fact]
    public void GetBooksQueryValidator_RejectsInvalidPageSizeAndSortField()
    {
        var validator = new GetBooksQueryValidator();

        var result = validator.Validate(new GetBooksQuery(
            null,
            null,
            null,
            null,
            null,
            null,
            "drop table books",
            "sideways",
            1,
            500));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetBooksQuery.PageSize));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetBooksQuery.SortBy));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetBooksQuery.SortDirection));
    }

    [Fact]
    public void BorrowBookRequestValidator_RejectsMissingBookId()
    {
        var validator = new BorrowBookRequestValidator();

        var result = validator.Validate(new BorrowBookRequest(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(BorrowBookRequest.BookId));
    }

    [Fact]
    public void BorrowingHistoryQueryValidator_RejectsInvalidStatusAndSort()
    {
        var validator = new BorrowingHistoryQueryValidator();

        var result = validator.Validate(new BorrowingHistoryQuery("Lost", "user.password", null, 0, 101));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(BorrowingHistoryQuery.Status));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(BorrowingHistoryQuery.Sort));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(BorrowingHistoryQuery.Page));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(BorrowingHistoryQuery.PageSize));
    }
}

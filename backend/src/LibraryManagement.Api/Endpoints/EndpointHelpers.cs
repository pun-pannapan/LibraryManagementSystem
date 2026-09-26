using System.Security.Claims;
using FluentValidation.Results;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Endpoints;

public static class EndpointHelpers
{
    public static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(value, out userId);
    }

    public static IResult ValidationProblem(params (string Field, string Error)[] errors)
    {
        var dictionary = errors
            .GroupBy(error => error.Field)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Error).ToArray());

        return Results.ValidationProblem(dictionary);
    }

    public static IResult? ValidationProblem(ValidationResult validationResult)
    {
        if (validationResult.IsValid)
        {
            return null;
        }

        var dictionary = validationResult.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return Results.ValidationProblem(dictionary);
    }

    public static BookDto ToDto(Book book) => new(
        book.Id,
        book.Isbn,
        book.Title,
        book.Author,
        book.Publisher,
        book.PublishedYear,
        book.ShelfCode,
        book.Location,
        book.CategoryId,
        book.Category.Name,
        book.AvailabilityStatus,
        Convert.ToBase64String(book.RowVersion));

    public static BorrowTransactionDto ToDto(BorrowTransaction transaction, string borrowerEmail) => new(
        transaction.Id,
        transaction.BookId,
        transaction.Book.Title,
        transaction.UserId,
        borrowerEmail,
        transaction.BorrowedAtUtc,
        transaction.DueAtUtc,
        transaction.ReturnedAtUtc,
        transaction.Status,
        transaction.RequestedAtUtc,
        transaction.AssignedAtUtc,
        transaction.ReturnRequestedAtUtc,
        transaction.AssignedByUserId,
        transaction.ProcessedByUserId,
        transaction.RejectedAtUtc,
        transaction.RejectedByUserId,
        transaction.CancelledAtUtc);

    public static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}

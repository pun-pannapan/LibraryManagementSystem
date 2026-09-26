using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(LibraryDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var categoryDefinitions = new[]
        {
            new CategoryDefinition("Fiction", "Novels and fictional works."),
            new CategoryDefinition("Science", "Science, technology, and engineering."),
            new CategoryDefinition("History", "Historical works and biographies.")
        };

        foreach (var definition in categoryDefinitions)
        {
            if (!await dbContext.Categories.AnyAsync(category => category.Name == definition.Name, cancellationToken))
            {
                dbContext.Categories.Add(new Category
                {
                    Name = definition.Name,
                    Description = definition.Description
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var categories = await dbContext.Categories
            .ToDictionaryAsync(category => category.Name, cancellationToken);

        var bookDefinitions = new[]
        {
            new BookDefinition("9780140449136", "The Odyssey", "Homer", "Penguin Classics", 1996, "Fiction"),
            new BookDefinition("9780132350884", "Clean Code", "Robert C. Martin", "Prentice Hall", 2008, "Science"),
            new BookDefinition("9780199232963", "The Making of the Modern World", "Alan Macfarlane", "Oxford University Press", 2006, "History")
        };

        foreach (var definition in bookDefinitions)
        {
            if (!await dbContext.Books.AnyAsync(book => book.Isbn == definition.Isbn, cancellationToken))
            {
                dbContext.Books.Add(new Book
                {
                    Isbn = definition.Isbn,
                    Title = definition.Title,
                    Author = definition.Author,
                    Publisher = definition.Publisher,
                    PublishedYear = definition.PublishedYear,
                    CategoryId = categories[definition.CategoryName].Id
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record CategoryDefinition(string Name, string Description);

    private sealed record BookDefinition(
        string Isbn,
        string Title,
        string Author,
        string Publisher,
        int PublishedYear,
        string CategoryName);
}

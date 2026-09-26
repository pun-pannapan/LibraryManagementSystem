using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Identity;
using LibraryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(configuration, userManager);

        var categoryDefinitions = new[]
        {
            new CategoryDefinition("Fiction", "Novels and fictional works."),
            new CategoryDefinition("Technology", "Software, systems, and engineering."),
            new CategoryDefinition("Business", "Business, management, and economics.")
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
            new BookDefinition("9780140449136", "The Odyssey", "Homer", "Penguin Classics", 1996, "Fiction", "FIC-01", "Main Library - Floor 1"),
            new BookDefinition("9780132350884", "Clean Code", "Robert C. Martin", "Prentice Hall", 2008, "Technology", "TEC-01", "Main Library - Floor 2"),
            new BookDefinition("9780201616224", "The Pragmatic Programmer", "Andrew Hunt and David Thomas", "Addison-Wesley", 1999, "Technology", "TEC-02", "Main Library - Floor 2"),
            new BookDefinition("9780066620992", "Good to Great", "Jim Collins", "HarperBusiness", 2001, "Business", "BUS-01", "Main Library - Floor 1"),
            new BookDefinition("9780134494166", "Effective Modern C++", "Scott Meyers", "O'Reilly Media", 2014, "Technology", "TEC-03", "Main Library - Floor 2"),
            new BookDefinition("9780135957059", "The Pragmatic Programmer (20th Anniversary)", "David Thomas and Andrew Hunt", "Addison-Wesley", 2019, "Technology", "TEC-04", "Main Library - Floor 2"),
            new BookDefinition("9781491950357", "Designing Data-Intensive Applications", "Martin Kleppmann", "O'Reilly Media", 2017, "Technology", "TEC-05", "Main Library - Floor 2"),
            new BookDefinition("9781449337711", "JavaScript: The Good Parts", "Douglas Crockford", "O'Reilly Media", 2008, "Technology", "TEC-06", "Main Library - Floor 2"),
            new BookDefinition("9780321125217", "Domain-Driven Design", "Eric Evans", "Addison-Wesley", 2003, "Technology", "TEC-07", "Main Library - Floor 2"),
            new BookDefinition("9780062315007", "The Lean Startup", "Eric Ries", "Crown Business", 2011, "Business", "BUS-02", "Main Library - Floor 1"),
            new BookDefinition("9780307887894", "The Personal MBA", "Josh Kaufman", "Portfolio", 2010, "Business", "BUS-03", "Main Library - Floor 1"),
            new BookDefinition("9780399588174", "Atomic Habits", "James Clear", "Avery", 2018, "Business", "BUS-04", "Main Library - Floor 1")
        };

        foreach (var definition in bookDefinitions)
        {
            var existingBook = await dbContext.Books.SingleOrDefaultAsync(book => book.Isbn == definition.Isbn, cancellationToken);
            if (existingBook is null)
            {
                dbContext.Books.Add(new Book
                {
                    Isbn = definition.Isbn,
                    Title = definition.Title,
                    Author = definition.Author,
                    Publisher = definition.Publisher,
                    PublishedYear = definition.PublishedYear,
                    ShelfCode = definition.ShelfCode,
                    Location = definition.Location,
                    CategoryId = categories[definition.CategoryName].Id
                });
            }
            else
            {
                existingBook.ShelfCode ??= definition.ShelfCode;
                existingBook.Location ??= definition.Location;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in new[] { ApplicationRoles.Administrator, ApplicationRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to seed role '{role}': {Describe(result)}");
                }
            }
        }
    }

    private static async Task SeedUsersAsync(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        await SeedUserAsync(
            userManager,
            configuration["Seed:AdminEmail"],
            configuration["Seed:AdminPassword"],
            ApplicationRoles.Administrator,
            "Demo",
            "Admin");

        await SeedUserAsync(
            userManager,
            configuration["Seed:UserEmail"],
            configuration["Seed:UserPassword"],
            ApplicationRoles.User,
            "Demo",
            "User");
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string? email,
        string? password,
        string role,
        string firstName,
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed user '{email}': {Describe(createResult)}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to assign role '{role}' to '{email}': {Describe(roleResult)}");
            }
        }
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));

    private sealed record CategoryDefinition(string Name, string Description);

    private sealed record BookDefinition(
        string Isbn,
        string Title,
        string Author,
        string Publisher,
        int PublishedYear,
        string CategoryName,
        string? ShelfCode = null,
        string? Location = null);
}

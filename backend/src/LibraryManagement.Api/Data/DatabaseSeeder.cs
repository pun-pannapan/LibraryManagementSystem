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
            new BookDefinition("9780140449136", "The Odyssey", "Homer", "Penguin Classics", 1996, "Fiction"),
            new BookDefinition("9780132350884", "Clean Code", "Robert C. Martin", "Prentice Hall", 2008, "Technology"),
            new BookDefinition("9780201616224", "The Pragmatic Programmer", "Andrew Hunt and David Thomas", "Addison-Wesley", 1999, "Technology"),
            new BookDefinition("9780066620992", "Good to Great", "Jim Collins", "HarperBusiness", 2001, "Business")
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
        string CategoryName);
}

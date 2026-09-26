using FluentAssertions;
using LibraryManagement.Domain.Entities;
using LibraryManagement.Infrastructure.Identity;
using LibraryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LibraryManagement.Api.Tests;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    public static readonly Guid TechnologyCategoryId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid CleanCodeBookId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid PragmaticProgrammerBookId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    private readonly string databaseName = $"LibraryManagementTests-{Guid.NewGuid()}";

    public TestApplicationFactory()
    {
        Set("Cors__AllowedOrigins", "http://localhost:4200");
        Set("Database__Server", "test");
        Set("Database__Name", "LibraryManagementTests");
        Set("Database__User", "sa");
        Set("Database__Password", "Password123!");
        Set("Jwt__Issuer", "LibraryManagement.Tests");
        Set("Jwt__Audience", "LibraryManagement.Tests.Client");
        Set("Jwt__Key", "IntegrationTestsJwtKeyWithAtLeast32Chars");
        Set("Jwt__ExpiryMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "http://localhost:4200",
                ["Database:Server"] = "test",
                ["Database:Name"] = "LibraryManagementTests",
                ["Database:User"] = "sa",
                ["Database:Password"] = "Password123!",
                ["Jwt:Issuer"] = "LibraryManagement.Tests",
                ["Jwt:Audience"] = "LibraryManagement.Tests.Client",
                ["Jwt:Key"] = "IntegrationTestsJwtKeyWithAtLeast32Chars",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }

    private static void Set(string key, string value) =>
        Environment.SetEnvironmentVariable(key, value);

    public async Task SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        foreach (var role in new[] { ApplicationRoles.Administrator, ApplicationRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        await SeedUserAsync(userManager, "admin@example.com", "Password123!", ApplicationRoles.Administrator);
        await SeedUserAsync(userManager, "user@example.com", "Password123!", ApplicationRoles.User);

        var category = new Category
        {
            Id = TechnologyCategoryId,
            Name = "Technology",
            Description = "Software and systems."
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        dbContext.Books.AddRange(
            new Book
            {
                Id = CleanCodeBookId,
                Isbn = "9780132350884",
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Publisher = "Prentice Hall",
                PublishedYear = 2008,
                CategoryId = category.Id
            },
            new Book
            {
                Id = PragmaticProgrammerBookId,
                Isbn = "9780201616224",
                Title = "The Pragmatic Programmer",
                Author = "Andrew Hunt and David Thomas",
                Publisher = "Addison-Wesley",
                PublishedYear = 1999,
                CategoryId = category.Id
            });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = role
        };

        var createResult = await userManager.CreateAsync(user, password);
        createResult.Succeeded.Should().BeTrue();

        var roleResult = await userManager.AddToRoleAsync(user, role);
        roleResult.Succeeded.Should().BeTrue();
    }
}

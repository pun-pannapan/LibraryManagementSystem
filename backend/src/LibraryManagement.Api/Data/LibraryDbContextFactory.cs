using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using LibraryManagement.Infrastructure.Persistence;

namespace LibraryManagement.Api.Data;

public sealed class LibraryDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connectionString = DatabaseConnection.Create(configuration);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly("LibraryManagement.Api"))
            .Options;

        return new ApplicationDbContext(options);
    }
}

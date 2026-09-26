using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibraryManagement.Api.Data;

public sealed class LibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connectionString = DatabaseConnection.Create(configuration);

        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new LibraryDbContext(options);
    }
}

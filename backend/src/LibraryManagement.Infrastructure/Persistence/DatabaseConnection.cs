using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LibraryManagement.Infrastructure.Persistence;

public static class DatabaseConnection
{
    public static string Create(IConfiguration configuration)
    {
        var explicitConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(explicitConnection))
        {
            return new SqlConnectionStringBuilder(explicitConnection).ConnectionString;
        }

        string Required(string key) => !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException($"{key} is required.");

        return new SqlConnectionStringBuilder
        {
            DataSource = Required("Database:Server"),
            InitialCatalog = Required("Database:Name"),
            UserID = Required("Database:User"),
            Password = Required("Database:Password"),
            TrustServerCertificate = true,
            Encrypt = false,
            ConnectTimeout = 3
        }.ConnectionString;
    }
}

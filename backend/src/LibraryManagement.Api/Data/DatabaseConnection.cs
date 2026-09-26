using LibraryManagement.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace LibraryManagement.Api.Data;

public static class DatabaseConnection
{
    public static string Create(IConfiguration configuration) =>
        global::LibraryManagement.Infrastructure.Persistence.DatabaseConnection.Create(configuration);
}

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LibraryManagement.Api.Data;

public sealed class DatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private readonly string connectionString = DatabaseConnection.Create(configuration);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 2;
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("SQL Server is accepting queries.");
        }
        catch (Exception exception) when (exception is SqlException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("SQL Server is not ready.", exception);
        }
    }
}

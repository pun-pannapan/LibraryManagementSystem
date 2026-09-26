using LibraryManagement.Api.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

if (args.Contains("--healthcheck", StringComparer.OrdinalIgnoreCase))
{
    using var client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    try
    {
        using var response = await client.GetAsync("http://127.0.0.1:8080/health");
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
        Environment.ExitCode = 1;
    }

    return;
}

var builder = WebApplication.CreateBuilder(args);
var connectionString = DatabaseConnection.Create(builder.Configuration);
var corsAllowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

if (corsAllowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");
}

if (corsAllowedOrigins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
{
    throw new InvalidOperationException("Cors:AllowedOrigins must contain absolute HTTP or HTTPS origins.");
}

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(corsAllowedOrigins)
        .WithHeaders("Accept", "Authorization", "Content-Type")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"));
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    await InitializeDatabaseAsync(app);
}

app.MapGet("/", () => Results.Ok(new
{
    service = "LibraryManagement.Api",
    status = "running"
}));

var healthOptions = new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync
};

app.MapHealthChecks("/health", healthOptions);
app.MapHealthChecks("/api/v1/health", healthOptions);

static async Task InitializeDatabaseAsync(WebApplication application)
{
    await using var scope = application.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
}

static async Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            duration = entry.Value.Duration.TotalMilliseconds
        })
    };

    await context.Response.WriteAsJsonAsync(response);
}

app.Run();

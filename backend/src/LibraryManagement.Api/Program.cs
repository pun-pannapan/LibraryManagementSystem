using System.Text;
using System.Security.Claims;
using Asp.Versioning;
using LibraryManagement.Application;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Api.Data;
using LibraryManagement.Api.Endpoints;
using LibraryManagement.Api.ExceptionHandling;
using LibraryManagement.Api.Middleware;
using LibraryManagement.Infrastructure.Identity;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Persistence;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
ValidateJwtConfiguration(builder.Configuration);

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

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Library Management API",
        Version = "1.0",
        Description = "API v1 for authentication, book inventory, borrowing, returns, and transaction history."
    });

    options.DocInclusionPredicate((documentName, apiDescription) =>
        apiDescription.GroupName is null || apiDescription.GroupName == documentName);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste the JWT access token only. Do not include the 'Bearer ' prefix.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }
        ] = Array.Empty<string>()
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(corsAllowedOrigins)
        .WithHeaders("Accept", "Authorization", "Content-Type")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"));
});
builder.Services.AddHealthChecks()
    .AddCheck("application", () => HealthCheckResult.Healthy("Application is running."), tags: ["ready"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Library Management API v1");
    });
}

if (app.Environment.IsDevelopment())
{
    await InitializeDatabaseAsync(app);
}

app.MapGet("/", () => Results.Ok(new
{
    service = "LibraryManagement.Api",
    status = "running"
}))
.AllowAnonymous()
.WithTags("Status")
.Produces(StatusCodes.Status200OK);

app.MapAuthEndpoints();
app.MapBookEndpoints();
app.MapBorrowingEndpoints();
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapGet("/api/v{version:apiVersion}/categories", async (ApplicationDbContext dbContext, CancellationToken cancellationToken) =>
{
    var categories = await dbContext.Categories
        .AsNoTracking()
        .OrderBy(category => category.Name)
        .Select(category => new CategoryDto(category.Id, category.Name, category.Description))
        .ToListAsync(cancellationToken);

    return Results.Ok(categories);
})
.RequireAuthorization()
.WithApiVersionSet(versionSet)
.MapToApiVersion(new ApiVersion(1, 0))
.WithTags("Categories")
.Produces<IReadOnlyCollection<CategoryDto>>()
.Produces(StatusCodes.Status401Unauthorized);

var healthOptions = new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync
};

app.MapHealthChecks("/health", healthOptions)
    .AllowAnonymous()
    .WithTags("Health");
app.MapHealthChecks("/api/v1/health", healthOptions)
    .AllowAnonymous()
    .WithTags("Health");

static async Task InitializeDatabaseAsync(WebApplication application)
{
    await using var scope = application.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

static void ValidateJwtConfiguration(IConfiguration configuration)
{
    foreach (var key in new[] { "Jwt:Issuer", "Jwt:Audience", "Jwt:Key" })
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
        {
            throw new InvalidOperationException($"{key} must be configured.");
        }
    }

    if (Encoding.UTF8.GetByteCount(configuration["Jwt:Key"]!) < 32)
    {
        throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
    }

    if (configuration.GetValue("Jwt:ExpiryMinutes", 60) <= 0)
    {
        throw new InvalidOperationException("Jwt:ExpiryMinutes must be greater than zero.");
    }
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

public partial class Program;

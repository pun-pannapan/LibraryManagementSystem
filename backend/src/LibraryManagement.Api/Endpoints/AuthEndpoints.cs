using Asp.Versioning;
using FluentValidation;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Infrastructure.Identity;
using LibraryManagement.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .Produces<AuthResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .Produces<UserDto>()
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService tokens,
        IValidator<RegisterRequest> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Authentication");

        var validation = EndpointHelpers.ValidationProblem(await validator.ValidateAsync(request, cancellationToken));
        if (validation is not null)
        {
            return validation;
        }

        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            logger.LogInformation("Registration conflict for an existing email address");
            return Results.Conflict(new { message = "A user with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = NormalizeOptional(request.FirstName),
            LastName = NormalizeOptional(request.LastName)
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            logger.LogInformation("Registration failed identity validation");
            return Results.ValidationProblem(ToValidationErrors(result));
        }

        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.User);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            logger.LogError("Registration failed while assigning default role to user {UserId}", user.Id);
            return Results.Problem(
                "The account could not be assigned the default user role.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var response = await tokens.CreateTokenAsync(user, cancellationToken);
        logger.LogInformation("User {UserId} registered successfully", user.Id);
        return Results.Created("/api/v1/auth/me", response);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService tokens,
        IValidator<LoginRequest> validator,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Authentication");

        var validation = EndpointHelpers.ValidationProblem(await validator.ValidateAsync(request, cancellationToken));
        if (validation is not null)
        {
            logger.LogInformation("Login failed because credentials were incomplete");
            return validation;
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            logger.LogInformation("Login failed because user was not found or inactive");
            return Results.Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            logger.LogInformation("Login failed for user {UserId}", user.Id);
            return Results.Unauthorized();
        }

        logger.LogInformation("User {UserId} logged in successfully", user.Id);
        return Results.Ok(await tokens.CreateTokenAsync(user, cancellationToken));
    }

    private static async Task<IResult> MeAsync(
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        if (!EndpointHelpers.TryGetUserId(context.User, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return Results.Unauthorized();
        }

        var roles = await userManager.GetRolesAsync(user);
        return Results.Ok(new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.ToArray()));
    }

    private static Dictionary<string, string[]> ToValidationErrors(IdentityResult result) =>
        result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

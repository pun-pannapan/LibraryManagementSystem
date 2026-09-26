using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryManagement.Application.Contracts;
using LibraryManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace LibraryManagement.Api.Services;

public interface IJwtTokenService
{
    Task<AuthResponse> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}

public sealed class JwtTokenService(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager) : IJwtTokenService
{
    public async Task<AuthResponse> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var issuer = Required("Jwt:Issuer");
        var audience = Required("Jwt:Audience");
        var key = Required("Jwt:Key");
        var expiryMinutes = configuration.GetValue("Jwt:ExpiryMinutes", 60);
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");
        }

        if (expiryMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:ExpiryMinutes must be greater than zero.");
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc,
            new UserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                roles.ToArray()));
    }

    private string Required(string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} must be configured.");
        }

        return value;
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clinic.Application.Abstractions;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Infrastructure.Identity;

/// <summary>
/// Issues HS256 JWTs containing the user's id, name, email and role claims.
/// Roles go in as <see cref="ClaimTypes.Role"/> so <c>[Authorize(Roles=...)]</c>
/// and the frontend permission gate can read them directly.
/// </summary>
public class JwtTokenService(UserManager<ApplicationUser> users, IConfiguration config) : IJwtTokenService
{
    public async Task<string> IssueTokenAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] is { Length: > 0 } k
                ? k
                : throw new InvalidOperationException(
                    "Jwt:Key is not configured. Set it via the Jwt__Key environment variable or appsettings.Development.json.")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var issuer = config["Jwt:Issuer"] ?? "Clinic";
        var audience = config["Jwt:Audience"] ?? "Clinic";
        var minutes = int.TryParse(config["Jwt:ExpiresMinutes"], out var m) ? m : 480;

        var roles = await users.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("external_id", user.ExternalId.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new("name", user.FullName),
        };
        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new(ClaimTypes.Email, user.Email));
        foreach (var role in roles)
            claims.Add(new(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

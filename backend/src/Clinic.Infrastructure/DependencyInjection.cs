using System.Text;
using Clinic.Application.Abstractions;
using Clinic.Application.Common.Interfaces;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers EF Core, ASP.NET Identity, JWT auth and infrastructure services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Lenient defaults to match CARE's local dev seed (admin/admin). Tighten for production.
                options.Password.RequiredLength = 5;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireUppercase = false;
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] is { Length: > 0 } k
                ? k
                : throw new InvalidOperationException(
                    "Jwt:Key is not configured. Set it via the Jwt__Key environment variable or appsettings.Development.json.")));
        var issuer = config["Jwt:Issuer"] ?? "Clinic";
        var audience = config["Jwt:Audience"] ?? "Clinic";

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.Name,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        services.AddAuthorization();

        // Expose DbContext via the Application-layer abstraction.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}

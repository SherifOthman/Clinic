using System.Data.Common;
using Clinic.Domain.Entities;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clinic.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Uses SQLite in-memory database (shared connection, isolated from production database).
/// Seeds test data reliably with ASP.NET Identity.
/// Injects a dedicated test-only JWT signing key via <c>UseSetting</c> so tests
/// are fully isolated from appsettings.json and appsettings.Development.json.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Inject a dedicated test-only JWT key so tests are fully isolated from
        // appsettings.json and appsettings.Development.json. The key is never used
        // outside the test process and carries no security value.
        // UseSetting sets values at the IWebHostBuilder level, which is read before
        // appsettings.json and therefore wins during DI registration in AddInfrastructure.
        builder.UseSetting("Jwt:Key", "TestSecretKeyForJwtTokenGenerationMustBe32CharsMin!");

        builder.ConfigureServices(services =>
        {
            // Remove the real database registration
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Create shared SQLite in-memory connection (must stay open)
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add test database with shared connection
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });

        builder.UseEnvironment("Test");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Seeds test data into the database. Call this from test fixture initialization.
    /// </summary>
    public async Task SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Ensure database schema is created
        await db.Database.EnsureCreatedAsync();

        // Seed roles
        var roles = new[] { "Admin", "Doctor", "Receptionist" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName, Description = roleName });
            }
        }

        // Seed test admin user - same as DbInitializer for consistency
        const string testUsername = "admin";
        if (await userManager.FindByNameAsync(testUsername) == null)
        {
            var testUser = new ApplicationUser
            {
                UserName = testUsername,
                Email = "admin@clinic.local",
                FullName = "System Administrator",
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(testUser, "admin");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testUser, "Admin");
            }
        }
    }
}

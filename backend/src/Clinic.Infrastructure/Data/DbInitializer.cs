using Clinic.Domain;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Infrastructure.Data;

/// <summary>
/// Runs on startup: applies migrations and seeds the system roles plus an admin
/// user so the app is usable immediately after first run. Per-feature seed data
/// (sample staff/patients) is added in later milestones' own seeders.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<ApplicationRole>>();

        await db.Database.MigrateAsync(ct);
        await SeedRolesAsync(roles);
        await SeedAdminAsync(users);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roles)
    {
        foreach (var name in Roles.All)
        {
            if (!await roles.RoleExistsAsync(name))
                await roles.CreateAsync(new ApplicationRole { Name = name, Description = name });
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> users)
    {
        const string adminUserName = "admin";
        if (await users.Users.AnyAsync(u => u.UserName == adminUserName)) return;

        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = "admin@clinic.local",
            FullName = "System Administrator",
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await users.CreateAsync(admin, "admin");
        if (result.Succeeded)
            await users.AddToRoleAsync(admin, Roles.Admin);
    }
}

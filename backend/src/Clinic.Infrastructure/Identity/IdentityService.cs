using Clinic.Application.Common.Interfaces;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Identity;

/// <summary>
/// Thin wrapper over ASP.NET Identity's UserManager/SignInManager/RoleManager so
/// the Application layer can authenticate and manage users without referencing
/// Identity types directly.
/// </summary>
public class IdentityService(
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles,
    SignInManager<ApplicationUser> signIn) : IIdentityService
{
    public Task<ApplicationUser?> FindByNameAsync(string userName) => users.FindByNameAsync(userName);

    public async Task<bool> CheckPasswordSignInAsync(ApplicationUser user, string password)
    {
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return result.Succeeded;
    }

    public Task<IList<string>> GetRolesAsync(ApplicationUser user) => users.GetRolesAsync(user);

    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string role) => users.GetUsersInRoleAsync(role);

    public async Task<(bool Success, string[] Errors)> CreateAsync(
        ApplicationUser user, string password, IEnumerable<string> roleNames)
    {
        var create = await users.CreateAsync(user, password);
        if (!create.Succeeded) return (false, create.Errors.Select(e => e.Description).ToArray());

        var validRoles = new List<string>();
        foreach (var r in roleNames.Distinct())
            if (await roles.RoleExistsAsync(r)) validRoles.Add(r);

        if (validRoles.Count != 0)
        {
            var add = await users.AddToRolesAsync(user, validRoles);
            if (!add.Succeeded) return (false, add.Errors.Select(e => e.Description).ToArray());
        }
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, string[] Errors)> UpdateAsync(ApplicationUser user)
    {
        var result = await users.UpdateAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Success, string[] Errors)> ResetPasswordAsync(ApplicationUser user, string newPassword)
    {
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var result = await users.ResetPasswordAsync(user, token, newPassword);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Success, string[] Errors)> AddToRolesAsync(ApplicationUser user, IEnumerable<string> roleNames)
    {
        var valid = new List<string>();
        foreach (var r in roleNames)
            if (await roles.RoleExistsAsync(r)) valid.Add(r);

        var result = await users.AddToRolesAsync(user, valid);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<(bool Success, string[] Errors)> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roleNames)
    {
        var result = await users.RemoveFromRolesAsync(user, roleNames);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToArray());
    }
}

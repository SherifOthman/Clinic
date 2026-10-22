using Clinic.Domain.Entities;

namespace Clinic.Application.Common.Interfaces;

/// <summary>
/// Abstraction over ASP.NET Identity's UserManager/SignInManager so auth
/// handlers stay in the Application layer without depending on Identity types.
/// </summary>
public interface IIdentityService
{
    Task<ApplicationUser?> FindByNameAsync(string userName);
    Task<bool> CheckPasswordSignInAsync(ApplicationUser user, string password);
    Task<IList<string>> GetRolesAsync(ApplicationUser user);
    Task<IList<ApplicationUser>> GetUsersInRoleAsync(string role);
    Task<(bool Success, string[] Errors)> CreateAsync(ApplicationUser user, string password, IEnumerable<string> roles);
    Task<(bool Success, string[] Errors)> UpdateAsync(ApplicationUser user);
    Task<(bool Success, string[] Errors)> ResetPasswordAsync(ApplicationUser user, string newPassword);
    Task<(bool Success, string[] Errors)> AddToRolesAsync(ApplicationUser user, IEnumerable<string> roles);
    Task<(bool Success, string[] Errors)> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles);
}

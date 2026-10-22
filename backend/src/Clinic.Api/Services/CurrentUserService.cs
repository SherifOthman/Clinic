using System.Security.Claims;
using Clinic.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Clinic.Api.Services;

/// <summary>Resolves the current user from the JWT claims via <c>IHttpContextAccessor</c>.</summary>
public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public int? UserId =>
        int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}

namespace Clinic.Application.Abstractions;

/// <summary>
/// Resolves the current user id/username/roles from the request context.
/// Implemented in the API layer using <c>IHttpContextAccessor</c>.
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

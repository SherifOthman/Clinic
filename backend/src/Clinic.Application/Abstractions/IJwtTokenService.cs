using Clinic.Domain.Entities;

namespace Clinic.Application.Abstractions;

public interface IJwtTokenService
{
    /// <summary>Issue a JWT for the given user (and their roles).</summary>
    Task<string> IssueTokenAsync(ApplicationUser user, CancellationToken ct = default);
}

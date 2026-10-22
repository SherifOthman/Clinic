using System.Security.Claims;

namespace Clinic.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so Application handlers never
/// depend on EF/Identity directly. Implemented by <c>ApplicationDbContext</c>
/// in Infrastructure. Domain entity DbSets are added here as features land.
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Set on save: audit fields + soft-delete bookkeeping.</summary>
    void SetAuditContext(ClaimsPrincipal? user);
}

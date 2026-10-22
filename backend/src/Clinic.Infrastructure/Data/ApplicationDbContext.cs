using System.Linq.Expressions;
using System.Security.Claims;
using Clinic.Application.Common.Interfaces;
using Clinic.Domain.Common;
using Clinic.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Clinic.Infrastructure.Data;

/// <summary>
/// Root DbContext. Uses ASP.NET Identity (<see cref="IdentityDbContext{TUser,TRole,TKey}"/>)
/// and implements <see cref="IApplicationDbContext"/> so Application handlers depend on the
/// abstraction, not EF. Handles audit fields and soft delete on save, and applies a global
/// query filter so soft-deleted <see cref="AuditableEntity"/> rows are hidden by default.
/// </summary>
public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, int>, IApplicationDbContext
{
    private ClaimsPrincipal? _auditPrincipal;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Domain entity DbSets are added here as features land (Patient, Encounter, ...).

    public void SetAuditContext(ClaimsPrincipal? user) => _auditPrincipal = user;

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ApplyAuditAndSoftDelete();
        return await base.SaveChangesAsync(ct);
    }

    public override int SaveChanges() => throw new NotSupportedException("Use the async overload.");

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Global query filter: hide soft-deleted entities from all queries.
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType)) continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Equal(
                Expression.Property(param, nameof(AuditableEntity.IsDeleted)),
                Expression.Constant(false));
            var lambda = Expression.Lambda(body, param);
            builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    private void ApplyAuditAndSoftDelete()
    {
        var user = _auditPrincipal?.Identity?.Name;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is AuditableEntity add)
                    {
                        add.CreatedAt = now;
                        if (add.CreatedBy is null) add.CreatedBy = user;
                        if (add.ExternalId == Guid.Empty) add.ExternalId = Guid.NewGuid();
                    }
                    break;

                case EntityState.Modified:
                    if (entry.Entity is AuditableEntity mod)
                    {
                        mod.UpdatedAt = now;
                        mod.UpdatedBy = user;
                    }
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is AuditableEntity del)
                    {
                        entry.State = EntityState.Modified;
                        del.IsDeleted = true;
                        del.UpdatedAt = now;
                        del.UpdatedBy = user;
                    }
                    break;
            }
        }
    }
}

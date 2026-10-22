namespace Clinic.Domain.Common;

/// <summary>
/// Base for all domain entities. Mirrors CARE's <c>BaseModel</c>: a public
/// <see cref="ExternalId"/> (Guid) for API lookups, audit timestamps, who
/// created/last updated the row, and soft delete enforced via EF Core's
/// global query filter (see <c>ApplicationDbContext</c>).
/// </summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }

    /// <summary>Stable public identifier used in API routes/responses.</summary>
    public Guid ExternalId { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}

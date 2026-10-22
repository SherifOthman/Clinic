using Microsoft.AspNetCore.Identity;

namespace Clinic.Domain.Entities;

/// <summary>
/// Staff user. Uses ASP.NET Identity (<see cref="IdentityUser{TKey}"/>) with int
/// primary keys. CARE's extra profile fields (full name, phone, doctor metadata)
/// are added here as plain columns.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    /// <summary>Stable public identifier used in API responses (mirrors CARE's external_id).</summary>
    public Guid ExternalId { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    /// <summary>Doctor-only fields; null for non-clinical staff.</summary>
    public string? Qualification { get; set; }

    public string? MedicalCouncilRegistration { get; set; }

    public DateTime? DoctorExperienceCommencedOn { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

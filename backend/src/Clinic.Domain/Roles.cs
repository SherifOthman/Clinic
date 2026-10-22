namespace Clinic.Domain;

/// <summary>
/// Known system roles. Stored as ASP.NET Identity roles; these constants keep
/// the names consistent across backend authorization and frontend gating.
/// Mirrors CARE's system roles in <c>care/security/roles/role.py</c>.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Nurse = "Nurse";
    public const string Staff = "Staff";
    public const string Pharmacist = "Pharmacist";
    public const string Receptionist = "Receptionist";

    /// <summary>All seed roles created on first run, in display order.</summary>
    public static readonly string[] All =
        { Admin, Doctor, Nurse, Staff, Pharmacist, Receptionist };
}

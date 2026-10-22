using Microsoft.AspNetCore.Identity;

namespace Clinic.Domain.Entities;

/// <summary>Identity role with int primary keys.</summary>
public class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }
}

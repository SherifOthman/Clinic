using Clinic.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Clinic.Application.Tests.Domain;

/// <summary>
/// Tests for ApplicationUser entity.
/// Verified against actual implementation in Clinic.Domain/Entities/ApplicationUser.cs
/// </summary>
public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_WhenCreated_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert - Verified defaults from actual code
        user.ExternalId.Should().NotBeEmpty(); // Property initializer: = Guid.NewGuid()
        user.IsActive.Should().BeTrue(); // Property initializer: = true
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)); // Property initializer: = DateTime.UtcNow
    }

    [Fact]
    public void ApplicationUser_WithFullData_ShouldStoreAllProperties()
    {
        // Arrange
        var externalId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var user = new ApplicationUser
        {
            ExternalId = externalId,
            FullName = "Dr. Ahmed Hassan",
            Email = "ahmed.hassan@clinic.local",
            UserName = "ahmed.hassan",
            Phone = "+201234567890",
            Gender = "Male",
            DateOfBirth = new DateTime(1985, 5, 15),
            Qualification = "MD",
            MedicalCouncilRegistration = "MC12345",
            DoctorExperienceCommencedOn = new DateTime(2010, 1, 1),
            IsActive = true,
            CreatedAt = createdAt
        };

        // Assert
        user.ExternalId.Should().Be(externalId);
        user.FullName.Should().Be("Dr. Ahmed Hassan");
        user.Email.Should().Be("ahmed.hassan@clinic.local");
        user.UserName.Should().Be("ahmed.hassan");
        user.Phone.Should().Be("+201234567890");
        user.Gender.Should().Be("Male");
        user.DateOfBirth.Should().Be(new DateTime(1985, 5, 15));
        user.Qualification.Should().Be("MD");
        user.MedicalCouncilRegistration.Should().Be("MC12345");
        user.DoctorExperienceCommencedOn.Should().Be(new DateTime(2010, 1, 1));
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void ApplicationUser_WithNullableFields_ShouldAllowNulls()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            FullName = "Reception Staff",
            UserName = "reception",
            Phone = null,
            Gender = null,
            DateOfBirth = null,
            Qualification = null,
            MedicalCouncilRegistration = null,
            DoctorExperienceCommencedOn = null
        };

        // Assert - Verify nullable fields accept nulls
        user.Phone.Should().BeNull();
        user.Gender.Should().BeNull();
        user.DateOfBirth.Should().BeNull();
        user.Qualification.Should().BeNull();
        user.MedicalCouncilRegistration.Should().BeNull();
        user.DoctorExperienceCommencedOn.Should().BeNull();
    }

    [Fact]
    public void ApplicationUser_ExternalId_ShouldBeUniqueForEachInstance()
    {
        // Arrange & Act
        var user1 = new ApplicationUser { FullName = "User 1", UserName = "user1" };
        var user2 = new ApplicationUser { FullName = "User 2", UserName = "user2" };

        // Assert - Property initializer generates unique GUIDs
        user1.ExternalId.Should().NotBe(user2.ExternalId);
        user1.ExternalId.Should().NotBeEmpty();
        user2.ExternalId.Should().NotBeEmpty();
    }
}

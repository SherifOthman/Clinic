using System.Net;
using System.Net.Http.Json;
using Clinic.Application.Features.Auth;
using FluentAssertions;
using Xunit;

namespace Clinic.Api.Tests.Controllers;

/// <summary>
/// Integration tests for AuthController.
/// Verified against actual implementation:
/// - Route: /api/auth/login (POST), /api/auth/me (GET)
/// - Response: AuthResult(string Token, UserBrief User)
/// - Error: AppException → 400 Bad Request
/// - Test user: admin/admin (seeded in TestWebApplicationFactory)
/// </summary>
public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Seed test data once per test class
        await _factory.SeedTestDataAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200AndToken()
    {
        // Arrange - Using seeded test user (admin/admin)
        var command = new LoginCommand("admin", "admin");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
        
        // Verify UserBrief structure (actual response shape)
        result.User.Should().NotBeNull();
        result.User.Username.Should().Be("admin");
        result.User.FullName.Should().Be("System Administrator"); // DbInitializer creates "System Administrator"
        result.User.Email.Should().Be("admin@clinic.local"); // DbInitializer uses "admin@clinic.local"
        result.User.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ShouldReturn400()
    {
        // Arrange
        var command = new LoginCommand("nonexistent", "anypassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert - AppException throws 400, not 401
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn400()
    {
        // Arrange
        var command = new LoginCommand("admin", "wrongpassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert - AppException throws 400, not 401
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturn401()
    {
        // Act - No Authorization header
        var response = await _client.GetAsync("/api/auth/me");

        // Assert - JWT middleware returns 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturn200AndUserInfo()
    {
        // Arrange - Login to get valid token
        var loginCommand = new LoginCommand("admin", "admin");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();
        
        // Create a new request with Authorization header
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        result.Should().NotBeNull();
        result!.User.Username.Should().Be("admin");
    }
}

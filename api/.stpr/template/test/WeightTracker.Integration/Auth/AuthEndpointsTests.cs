using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Auth;

public class AuthEndpointsTests : IntegrationTest
{
    public AuthEndpointsTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Workouts_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/workouts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Workouts_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync("/workouts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldReturnTheDocumentedFieldNames()
    {
        // Act
        var response = await Client.PostAsJsonAsync(
            "/auth/login", new { email = SeedCredentials.User, password = SeedCredentials.Password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.TryGetProperty("accessToken", out var accessToken).Should().BeTrue();
        payload.TryGetProperty("refreshToken", out var refreshToken).Should().BeTrue();
        payload.TryGetProperty("expiresIn", out var expiresIn).Should().BeTrue();

        accessToken.GetString().Should().NotBeNullOrWhiteSpace();
        refreshToken.GetString().Should().NotBeNullOrWhiteSpace();
        expiresIn.GetInt32().Should().Be(900);
    }

    [Fact]
    public async Task Login_ShouldRejectAWrongPassword()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync(
            "/auth/login", new { email = SeedCredentials.User, password = "not-the-password" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldRejectAnUnknownEmail()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync(
            "/auth/login", new { email = "nobody@weighttracker.test", password = SeedCredentials.Password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldRejectAMalformedEmailWithoutLeakingThatItIsMalformed()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync(
            "/auth/login", new { email = "not-an-email", password = SeedCredentials.Password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ShouldRejectAMissingPasswordField()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync("/auth/login", new { email = SeedCredentials.User });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ShouldReturnANewCredentialPair()
    {
        // Arrange
        var login = await Login(SeedCredentials.User);
        var originalRefreshToken = login.GetProperty("refreshToken").GetString();

        // Act
        var response = await Client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefreshToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("refreshToken").GetString().Should().NotBe(originalRefreshToken);
        payload.GetProperty("expiresIn").GetInt32().Should().Be(900);
    }

    [Fact]
    public async Task Refresh_ShouldRejectAReplayOfTheRotatedToken()
    {
        // Arrange
        var login = await Login(SeedCredentials.User);
        var originalRefreshToken = login.GetProperty("refreshToken").GetString();

        var first = await Client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefreshToken });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var replay = await Client.PostAsJsonAsync("/auth/refresh", new { refreshToken = originalRefreshToken });

        // Assert
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldIssueATokenThatStillWorksOnTheNextRotation()
    {
        // Arrange
        var login = await Login(SeedCredentials.User);

        var rotated = await Client.PostAsJsonAsync(
            "/auth/refresh", new { refreshToken = login.GetProperty("refreshToken").GetString() });
        var rotatedPayload = await rotated.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        // Act
        var second = await Client.PostAsJsonAsync(
            "/auth/refresh", new { refreshToken = rotatedPayload.GetProperty("refreshToken").GetString() });

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ShouldRejectAnUnknownToken()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync("/auth/refresh", new { refreshToken = "never-issued" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessTokenFromRefresh_ShouldAuthenticateAgainstWorkouts()
    {
        // Arrange
        var login = await Login(SeedCredentials.User);

        var refreshed = await Client.PostAsJsonAsync(
            "/auth/refresh", new { refreshToken = login.GetProperty("refreshToken").GetString() });
        var payload = await refreshed.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        Client.DefaultRequestHeaders.Add(
            "Authorization", $"Bearer {payload.GetProperty("accessToken").GetString()}");

        // Act
        var workouts = await Client.GetAsync("/workouts");

        // Assert
        workouts.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

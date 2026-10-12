using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
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

    private const string NewUserEmail = "new.user@weighttracker.test";
    private const string NewUserPassword = "Sup3rSecret!";

    private Task<HttpResponseMessage> Signup(object body) => Client.PostAsJsonAsync("/auth/signup", body);

    [Fact]
    public async Task Signup_ShouldReturnTheDocumentedFieldNames()
    {
        // Arrange & Act
        var response = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("expiresIn").GetInt32().Should().Be(900);
    }

    [Fact]
    public async Task Signup_ShouldAllowLoggingInWithTheNewCredentials()
    {
        // Arrange
        var signup = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = 1 });
        signup.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var login = await Client.PostAsJsonAsync("/auth/login", new { email = NewUserEmail, password = NewUserPassword });

        // Assert
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Signup_ShouldReturnATokenThatAuthenticatesAgainstWorkouts()
    {
        // Arrange
        var signup = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = 1 });
        var payload = await signup.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        Client.DefaultRequestHeaders.Add(
            "Authorization", $"Bearer {payload.GetProperty("accessToken").GetString()}");

        // Act
        var workouts = await Client.GetAsync("/workouts");

        // Assert
        workouts.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Signup_ShouldReturnARefreshTokenThatCanBeRotated()
    {
        // Arrange
        var signup = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = 1 });
        var payload = await signup.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        // Act
        var refreshed = await Client.PostAsJsonAsync(
            "/auth/refresh", new { refreshToken = payload.GetProperty("refreshToken").GetString() });

        // Assert
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Signup_ShouldPersistTheRequestedRole()
    {
        // Arrange
        var signup = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = (int)Role.TRAINER });
        signup.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var user = await Factory.WithScope(services => services.GetRequiredService<IUserRepository>()
            .GetByEmail(Email.From(NewUserEmail), CancellationToken.None));

        // Assert
        user.Should().NotBeNull();
        user!.Role.Should().Be(Role.TRAINER);
    }

    [Fact]
    public async Task Signup_ShouldDefaultToTheUserRoleWhenRoleIsOmitted()
    {
        // Arrange
        var signup = await Signup(new { email = NewUserEmail, password = NewUserPassword });
        signup.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var user = await Factory.WithScope(services => services.GetRequiredService<IUserRepository>()
            .GetByEmail(Email.From(NewUserEmail), CancellationToken.None));

        // Assert
        user.Should().NotBeNull();
        user!.Role.Should().Be(Role.USER);
    }

    [Fact]
    public async Task Signup_ShouldRejectAnAlreadyRegisteredEmail()
    {
        // Arrange & Act
        var response = await Signup(new { email = SeedCredentials.User, password = NewUserPassword, role = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Signup_ShouldTreatEmailAsCaseInsensitive()
    {
        // Arrange & Act
        var response = await Signup(new { email = SeedCredentials.User.ToUpperInvariant(), password = NewUserPassword, role = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Signup_ShouldNotOverwriteAnExistingUserOnDuplicate()
    {
        // Arrange
        var duplicate = await Signup(new { email = SeedCredentials.User, password = NewUserPassword, role = 1 });
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Act
        var login = await Client.PostAsJsonAsync(
            "/auth/login", new { email = SeedCredentials.User, password = NewUserPassword });

        // Assert
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Signup_ShouldRejectAWeakPassword()
    {
        // Arrange & Act
        var response = await Signup(new { email = NewUserEmail, password = "weak", role = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.GetProperty("detail").GetString().Should().Contain("not strong enough");
    }

    [Fact]
    public async Task Signup_ShouldRejectTheAdminRole()
    {
        // Arrange & Act
        var response = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = (int)Role.ADMIN });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.GetProperty("detail").GetString().Should().Contain("Admin accounts cannot be created");
    }

    [Fact]
    public async Task Signup_ShouldNotCreateAnAdminAccount()
    {
        // Arrange
        var signup = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = (int)Role.ADMIN });
        signup.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act
        var user = await Factory.WithScope(services => services.GetRequiredService<IUserRepository>()
            .GetByEmail(Email.From(NewUserEmail), CancellationToken.None));

        // Assert
        user.Should().BeNull();
    }

    [Fact]
    public async Task Signup_ShouldRejectAMalformedEmail()
    {
        // Arrange & Act
        var response = await Signup(new { email = "not-an-email", password = NewUserPassword, role = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Signup_ShouldRejectAMissingPasswordField()
    {
        // Arrange & Act
        var response = await Signup(new { email = NewUserEmail, role = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_ShouldRejectAMissingEmailField()
    {
        // Arrange & Act
        var response = await Signup(new { password = NewUserPassword, role = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_ShouldRejectAnUndefinedRole()
    {
        // Arrange & Act
        var response = await Signup(new { email = NewUserEmail, password = NewUserPassword, role = 99 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

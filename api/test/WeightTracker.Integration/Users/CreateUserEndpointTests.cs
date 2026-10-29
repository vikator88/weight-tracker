using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Users;

public class CreateUserEndpointTests : IntegrationTest
{
    private const string Route = "/users";

    private const string NewUserEmail = "created.by.admin@weighttracker.test";
    private const string NewUserPassword = "Passw0rd!";

    private static readonly object ValidBody = new { email = NewUserEmail, password = NewUserPassword, role = (int)Role.USER };

    public CreateUserEndpointTests(ApiFactory factory) : base(factory)
    { }

    private Task<int> StoredUserCount()
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .Users.AsNoTracking().CountAsync());

    private Task<UserEntity> StoredUser(string email)
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .Users.AsNoTracking().SingleAsync(user => user.Email == email));

    private Task<bool> NewUserExists()
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .Users.AsNoTracking().AnyAsync(user => user.Email == NewUserEmail));

    private async Task AssertNoUserWasCreated(int usersBefore)
    {
        (await StoredUserCount()).Should().Be(usersBefore);
        (await NewUserExists()).Should().BeFalse();
    }

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallersAndCreateNothing()
    {
        // Arrange
        var usersBefore = await StoredUserCount();

        // Act
        var response = await Client.PostAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertNoUserWasCreated(usersBefore);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageTokenAndCreateNothing()
    {
        // Arrange
        var usersBefore = await StoredUserCount();
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertNoUserWasCreated(usersBefore);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Trainer)]
    public async Task Post_ShouldForbidNonAdminCallersAndCreateNothing(string callerEmail)
    {
        // Arrange
        var client = await AuthenticatedClientFor(callerEmail);
        var usersBefore = await StoredUserCount();

        // Act
        var response = await client.PostAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertNoUserWasCreated(usersBefore);
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task Post_ShouldCreateAUserOfEachRole(Role role)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(
            Route, new { email = NewUserEmail, password = NewUserPassword, role = (int)role });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("email").GetString().Should().Be(NewUserEmail);
        body.GetProperty("role").GetString().Should().Be(role.ToString());

        var storedRole = await Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .Users.AsNoTracking().Where(user => user.Email == NewUserEmail).Select(user => user.Role).SingleAsync());
        storedRole.Should().Be((int)role);
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task Login_ShouldSucceedForAUserCreatedByAnAdmin(Role role)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var created = await admin.PostAsJsonAsync(
            Route, new { email = NewUserEmail, password = NewUserPassword, role = (int)role });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var login = await Client.PostAsJsonAsync("/auth/login", new { email = NewUserEmail, password = NewUserPassword });

        // Assert
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_ShouldCreateAUserWithOnlyTheRequiredFields()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("name").GetString().Should().BeEmpty();
        body.GetProperty("surname").GetString().Should().BeEmpty();
        body.GetProperty("dateBirth").GetString().Should().Be("0001-01-01");
    }

    [Fact]
    public async Task Post_ShouldEchoEveryGivenFieldWithTrimmedNames()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route, new
        {
            email = NewUserEmail,
            password = NewUserPassword,
            role = (int)Role.TRAINER,
            name = "  Margaret ",
            surname = " Hamilton  ",
            dateBirth = "1936-08-17",
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
        body.GetProperty("email").GetString().Should().Be(NewUserEmail);
        body.GetProperty("name").GetString().Should().Be("Margaret");
        body.GetProperty("surname").GetString().Should().Be("Hamilton");
        body.GetProperty("dateBirth").GetString().Should().Be("1936-08-17");
        body.GetProperty("role").GetString().Should().Be("TRAINER");
    }

    [Fact]
    public async Task Post_ShouldReturnExactlyTheUserFieldsAndNoAuthenticationData()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var properties = body.EnumerateObject().Select(property => property.Name).ToList();
        properties.Should().BeEquivalentTo("id", "email", "name", "surname", "dateBirth", "role");
    }

    [Fact]
    public async Task Post_ShouldNotStartASessionForTheCreatedUser()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var createdId = body.GetProperty("id").GetGuid();

        var refreshTokens = await Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .RefreshTokens.AsNoTracking().CountAsync(token => token.UserId == createdId));
        refreshTokens.Should().Be(0);
    }

    public static TheoryData<object> InvalidBodies => new()
    {
        new { email = "not-an-email", password = NewUserPassword, role = (int)Role.USER },
        new { email = NewUserEmail, password = "weak", role = (int)Role.USER },
        new { password = NewUserPassword, role = (int)Role.USER },
        new { email = NewUserEmail, role = (int)Role.USER },
        new { email = NewUserEmail, password = NewUserPassword },
        new { email = NewUserEmail, password = NewUserPassword, role = 99 },
        new { email = NewUserEmail, password = NewUserPassword, role = (int)Role.USER, name = "   " },
        new { email = NewUserEmail, password = NewUserPassword, role = (int)Role.USER, surname = new string('a', 101) },
    };

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Post_ShouldRejectInvalidInputAndCreateNothing(object requestBody)
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var usersBefore = await StoredUserCount();

        // Act
        var response = await admin.PostAsJsonAsync(Route, requestBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertNoUserWasCreated(usersBefore);
    }

    [Fact]
    public async Task Post_ShouldRejectAnExistingEmailAndLeaveTheExistingUserUnchanged()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var usersBefore = await StoredUserCount();
        var existingBefore = await StoredUser(SeedCredentials.User);

        // Act
        var response = await admin.PostAsJsonAsync(
            Route, new { email = SeedCredentials.User, password = "Different1!", role = (int)Role.ADMIN });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await StoredUserCount()).Should().Be(usersBefore);
        (await StoredUser(SeedCredentials.User)).Should().BeEquivalentTo(existingBefore);
    }
}

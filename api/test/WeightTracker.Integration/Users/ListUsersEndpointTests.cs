using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Users;

public class ListUsersEndpointTests : IntegrationTest
{
    private const string Route = "/users";

    public ListUsersEndpointTests(ApiFactory factory) : base(factory)
    { }

    private Task<List<string>> StoredEmails()
        => Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .Users.AsNoTracking().Select(user => user.Email).ToListAsync());

    private static async Task<List<JsonElement>> Items(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        return body.EnumerateArray().ToList();
    }

    private static async Task AssertNoUserDataIn(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain(SeedCredentials.User);
        content.Should().NotContain(SeedCredentials.Trainer);
        content.Should().NotContain(SeedCredentials.Admin);
    }

    [Fact]
    public async Task Get_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldForbidAnAthleteAndReturnNoUsers()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertNoUserDataIn(response);
    }

    [Fact]
    public async Task Get_ShouldAnswerNotImplementedToATrainerAndReturnNoUsers()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        await AssertNoUserDataIn(response);
    }

    [Fact]
    public async Task Get_ShouldListEveryUserOfEveryRoleForAnAdmin()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await Items(response);
        items.Select(item => item.GetProperty("email").GetString())
            .Should().BeEquivalentTo(await StoredEmails());
        items.Select(item => item.GetProperty("role").GetString())
            .Should().Contain(["USER", "TRAINER", "ADMIN"]);
        items.Select(item => item.GetProperty("id").GetGuid())
            .Should().Contain([SeedData.UserId, SeedData.TrainerId, SeedData.AdminId]);
    }

    [Fact]
    public async Task Get_ShouldIncludeAUserCreatedByAnAdmin()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var created = await admin.PostAsJsonAsync(
            Route, new { email = "listed.user@weighttracker.test", password = "Passw0rd!", role = (int)Role.USER });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await Items(response);
        items.Select(item => item.GetProperty("email").GetString())
            .Should().Contain("listed.user@weighttracker.test");
    }

    [Fact]
    public async Task Get_ShouldOrderUsersByEmailAscending()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var emails = (await Items(response)).Select(item => item.GetProperty("email").GetString()).ToList();
        emails.Should().HaveCountGreaterThan(1);
        emails.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task Get_ShouldExposeOnlyPublicProfileFields()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await Items(response);
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(item => item.EnumerateObject().Select(property => property.Name)
            .SequenceEqual(new[] { "id", "email", "name", "surname", "role", "isActive", "deactivatedAtUtc" }));
    }

    [Fact]
    public async Task Get_ShouldListEverySeededUserAsActive()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await Items(response);
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(item => item.GetProperty("isActive").GetBoolean()
            && item.GetProperty("deactivatedAtUtc").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_ShouldStillListADeactivatedUserWithItsState()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var deactivated = await admin.PutAsync($"/users/{SeedData.UserId}/deactivate", null);
        deactivated.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await admin.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = (await Items(response)).Single(item => item.GetProperty("id").GetGuid() == SeedData.UserId);
        user.GetProperty("isActive").GetBoolean().Should().BeFalse();
        user.GetProperty("deactivatedAtUtc").ValueKind.Should().Be(JsonValueKind.String);
    }
}

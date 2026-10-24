using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Users;

public class UserProfileEndpointTests : IntegrationTest
{
    private const string Route = "/users/me";

    private static readonly object ValidBody = new { name = "Grace", surname = "Hopper" };

    public UserProfileEndpointTests(ApiFactory factory) : base(factory)
    { }

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

    [Theory]
    [InlineData(SeedCredentials.User, "00000000-0000-0000-0000-000000000001", "Ada", "Lovelace", "USER")]
    [InlineData(SeedCredentials.Trainer, "00000000-0000-0000-0000-000000000002", "Grace", "Hopper", "TRAINER")]
    [InlineData(SeedCredentials.Admin, "00000000-0000-0000-0000-000000000003", "Alan", "Turing", "ADMIN")]
    public async Task Get_ShouldReturnTheCallersOwnProfile(
        string email, string id, string name, string surname, string role)
    {
        // Arrange
        var client = await AuthenticatedClientFor(email);

        // Act
        var response = await client.GetAsync(Route);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("email").GetString().Should().Be(email);
        body.GetProperty("name").GetString().Should().Be(name);
        body.GetProperty("surname").GetString().Should().Be(surname);
        body.GetProperty("role").GetString().Should().Be(role);
        body.GetProperty("id").GetGuid().Should().Be(Guid.Parse(id));
    }

    [Fact]
    public async Task Get_ShouldNotExposePrivateFieldsNorOtherUsers()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.GetAsync(Route);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var properties = body.EnumerateObject().Select(property => property.Name).ToList();
        properties.Should().BeEquivalentTo("id", "email", "name", "surname", "role");
        body.GetProperty("id").GetGuid().Should().Be(SeedData.AdminId);
        body.GetProperty("email").GetString().Should().Be(SeedCredentials.Admin);
    }

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallersAndChangeNothing()
    {
        // Arrange
        var owner = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await Client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertProfileIsUnchanged(owner);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageTokenAndChangeNothing()
    {
        // Arrange
        var owner = await AuthenticatedClientFor(SeedCredentials.User);
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertProfileIsUnchanged(owner);
    }

    [Fact]
    public async Task Put_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        await Factory.RemoveUser(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldReplaceNameAndSurnameAndPersistThem()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("name").GetString().Should().Be("Grace");
        body.GetProperty("surname").GetString().Should().Be("Hopper");
        body.GetProperty("email").GetString().Should().Be(SeedCredentials.User);
        body.GetProperty("role").GetString().Should().Be("USER");

        var stored = await client.GetFromJsonAsync<JsonElement>(Route, JsonOptions);
        stored.GetProperty("name").GetString().Should().Be("Grace");
        stored.GetProperty("surname").GetString().Should().Be("Hopper");
    }

    [Fact]
    public async Task Put_ShouldTrimSurroundingWhitespace()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route, new { name = "  Ana  ", surname = " Ruiz " });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("name").GetString().Should().Be("Ana");
        body.GetProperty("surname").GetString().Should().Be("Ruiz");

        var stored = await client.GetFromJsonAsync<JsonElement>(Route, JsonOptions);
        stored.GetProperty("name").GetString().Should().Be("Ana");
        stored.GetProperty("surname").GetString().Should().Be("Ruiz");
    }

    [Fact]
    public async Task Put_ShouldNotChangeAnyOtherUser()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var trainerClient = await AuthenticatedClientFor(SeedCredentials.Trainer);
        var adminClient = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(Route, ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var trainer = await trainerClient.GetFromJsonAsync<JsonElement>(Route, JsonOptions);
        trainer.GetProperty("name").GetString().Should().Be("Grace");
        trainer.GetProperty("surname").GetString().Should().Be("Hopper");

        var admin = await adminClient.GetFromJsonAsync<JsonElement>(Route, JsonOptions);
        admin.GetProperty("name").GetString().Should().Be("Alan");
        admin.GetProperty("surname").GetString().Should().Be("Turing");
    }

    [Fact]
    public async Task Put_ShouldRejectAMissingSurnameAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route, new { name = "Grace" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertProfileIsUnchanged(client);
    }

    [Fact]
    public async Task Put_ShouldRejectABlankNameAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route, new { name = "   ", surname = "Hopper" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertProfileIsUnchanged(client);
    }

    [Fact]
    public async Task Put_ShouldRejectATooLongSurnameAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route, new { name = "Grace", surname = new string('a', 101) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertProfileIsUnchanged(client);
    }

    [Fact]
    public async Task Put_ShouldIgnoreARoleInTheBody()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(Route, new { name = "Grace", surname = "Hopper", role = "ADMIN" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("role").GetString().Should().Be("USER");

        var stored = await client.GetFromJsonAsync<JsonElement>(Route, JsonOptions);
        stored.GetProperty("role").GetString().Should().Be("USER");
    }

    [Fact]
    public async Task Put_ShouldIgnoreAnEmailInTheBody()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PutAsJsonAsync(
            Route, new { name = "Grace", surname = "Hopper", email = "other@weighttracker.test" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("email").GetString().Should().Be(SeedCredentials.User);

        var login = await Client.PostAsJsonAsync(
            "/auth/login", new { email = SeedCredentials.User, password = SeedCredentials.Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task AssertProfileIsUnchanged(HttpClient client)
    {
        var stored = await client.GetFromJsonAsync<JsonElement>(Route, JsonOptions);
        stored.GetProperty("name").GetString().Should().Be("Ada");
        stored.GetProperty("surname").GetString().Should().Be("Lovelace");
    }
}

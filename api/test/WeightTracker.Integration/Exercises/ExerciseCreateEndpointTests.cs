using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

public class ExerciseCreateEndpointTests : IntegrationTest
{
    private static readonly object ValidBody =
        new { name = "Remo con barra", videoUrl = "https://videos.example.com/remo", bodyPart = 4 };

    public ExerciseCreateEndpointTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync("/exercises", ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync("/exercises", ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Trainer)]
    public async Task Post_ShouldForbidNonAdminCallersAndCreateNothing(string email)
    {
        // Arrange
        var client = await AuthenticatedClientFor(email);

        // Act
        var response = await client.PostAsJsonAsync("/exercises", ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var all = await Client.GetFromJsonAsync<JsonElement>("/exercises", JsonOptions);
        all.GetArrayLength().Should().Be(4);
    }

    [Fact]
    public async Task Post_ShouldCreateTheExerciseForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/exercises", ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = payload.GetProperty("id").GetGuid();

        id.Should().NotBeEmpty();
        payload.GetProperty("name").GetString().Should().Be("Remo con barra");
        payload.GetProperty("videoUrl").GetString().Should().Be("https://videos.example.com/remo");
        payload.GetProperty("bodyPart").GetString().Should().Be("BACK");
        response.Headers.Location!.AbsolutePath.Should().Be($"/exercises/{id}");
    }

    [Fact]
    public async Task Post_ShouldMakeTheCreatedExerciseReadable()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);
        var created = await (await client.PostAsJsonAsync("/exercises", ValidBody))
            .Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var id = created.GetProperty("id").GetGuid();

        // Act
        var response = await Client.GetAsync($"/exercises/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_ShouldAcceptAMissingVideoUrl()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/exercises", new { name = "Remo con barra", bodyPart = 4 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("videoUrl").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Post_ShouldRejectAMissingName()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/exercises", new { bodyPart = 4 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldRejectAnUndefinedBodyPart()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/exercises", new { name = "Remo con barra", bodyPart = 99 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("ab", null)]
    [InlineData("Remo con barra", "not a url")]
    [InlineData("Remo con barra", "ftp://videos.example.com/remo")]
    [InlineData("Remo con barra", "https://video")]
    public async Task Post_ShouldRejectBusinessInvalidInputAndCreateNothing(string name, string? videoUrl)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/exercises", new { name, videoUrl, bodyPart = 4 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var all = await Client.GetFromJsonAsync<JsonElement>("/exercises", JsonOptions);
        all.GetArrayLength().Should().Be(4);
    }
}

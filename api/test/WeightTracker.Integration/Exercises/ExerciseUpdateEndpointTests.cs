using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

public class ExerciseUpdateEndpointTests : IntegrationTest
{
    private static readonly object ValidBody =
        new { name = "Sentadilla Frontal", videoUrl = "https://videos.example.com/frontal", bodyPart = 2 };

    public ExerciseUpdateEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string Route(Guid id) => $"/exercises/{id}";

    [Fact]
    public async Task Put_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PutAsJsonAsync(Route(SeedData.SentadillaId), ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PutAsJsonAsync(Route(SeedData.SentadillaId), ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Trainer)]
    public async Task Put_ShouldForbidNonAdminCallersAndChangeNothing(string email)
    {
        // Arrange
        var client = await AuthenticatedClientFor(email);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.SentadillaId), ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var stored = await Client.GetFromJsonAsync<JsonElement>(Route(SeedData.SentadillaId), JsonOptions);
        stored.GetProperty("name").GetString().Should().Be("Sentadilla");
    }

    [Fact]
    public async Task Put_ShouldReplaceEveryAttributeForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.SentadillaId), ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        payload.GetProperty("id").GetGuid().Should().Be(SeedData.SentadillaId);
        payload.GetProperty("name").GetString().Should().Be("Sentadilla Frontal");
        payload.GetProperty("videoUrl").GetString().Should().Be("https://videos.example.com/frontal");
        payload.GetProperty("bodyPart").GetString().Should().Be("LEGS");
    }

    [Fact]
    public async Task Put_ShouldPersistTheChangeWithoutDuplicatingTheRow()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);
        await client.PutAsJsonAsync(Route(SeedData.SentadillaId), ValidBody);

        // Act
        var all = await Client.GetFromJsonAsync<JsonElement>("/exercises", JsonOptions);

        // Assert
        all.GetArrayLength().Should().Be(4);
        all.EnumerateArray().Count(exercise => exercise.GetProperty("id").GetGuid() == SeedData.SentadillaId)
            .Should().Be(1);
        all.EnumerateArray().Single(exercise => exercise.GetProperty("id").GetGuid() == SeedData.SentadillaId)
            .GetProperty("name").GetString().Should().Be("Sentadilla Frontal");
    }

    [Fact]
    public async Task Put_ShouldClearTheVideoUrlWhenItIsOmitted()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);
        await client.PutAsJsonAsync(Route(SeedData.SentadillaId), ValidBody);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.SentadillaId), new { name = "Sentadilla Frontal", bodyPart = 2 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = await Client.GetFromJsonAsync<JsonElement>(Route(SeedData.SentadillaId), JsonOptions);
        stored.GetProperty("videoUrl").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Put_ShouldReturnNotFoundForAnUnknownExerciseAndCreateNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);
        var unknown = Id.New().Value;

        // Act
        var response = await client.PutAsJsonAsync(Route(unknown), ValidBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Client.GetAsync(Route(unknown))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_ShouldRejectAMissingName()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.SentadillaId), new { bodyPart = 2 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("ab", null)]
    [InlineData("Sentadilla Frontal", "https://video")]
    public async Task Put_ShouldRejectBusinessInvalidInputAndChangeNothing(string name, string? videoUrl)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PutAsJsonAsync(Route(SeedData.SentadillaId), new { name, videoUrl, bodyPart = 2 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var stored = await Client.GetFromJsonAsync<JsonElement>(Route(SeedData.SentadillaId), JsonOptions);
        stored.GetProperty("name").GetString().Should().Be("Sentadilla");
    }
}

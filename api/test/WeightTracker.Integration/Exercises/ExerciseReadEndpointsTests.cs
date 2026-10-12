using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

/// <summary>
/// Both read routes are public, so no missing-token or invalid-token cases belong here.
/// </summary>
public class ExerciseReadEndpointsTests : IntegrationTest
{
    public ExerciseReadEndpointsTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task GetAll_ShouldReturnEverySeededExerciseToAnAnonymousCaller()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/exercises");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.EnumerateArray().Select(exercise => exercise.GetProperty("id").GetGuid())
            .Should().BeEquivalentTo(
                [SeedData.PressBancaId, SeedData.SentadillaId, SeedData.PlanchaId, SeedData.CurlBicepsId]);
    }

    [Fact]
    public async Task GetById_ShouldReturnTheExerciseToAnAnonymousCaller()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/exercises/{SeedData.PressBancaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.GetProperty("id").GetGuid().Should().Be(SeedData.PressBancaId);
        payload.GetProperty("name").GetString().Should().Be("Press Banca");
        payload.GetProperty("videoUrl").ValueKind.Should().Be(JsonValueKind.Null);
        payload.GetProperty("bodyPart").GetString().Should().Be("CHEST");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFoundForAnUnknownExercise()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/exercises/{Id.New().Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldRejectAnEmptyIdentifier()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/exercises/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

/// <summary>
/// Both read routes are public, so no missing-token or invalid-token cases belong here. The catalog is
/// read-only through the API, so the write cases only assert that nothing is served or changed.
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
    public async Task GetAll_ShouldReturnTranslationsMuscleAndBodyPartOfEveryExercise()
    {
        // Arrange & Act
        var payload = await Client.GetFromJsonAsync<JsonElement>("/exercises", JsonOptions);

        // Assert
        payload.EnumerateArray().Should().OnlyContain(exercise =>
            exercise.GetProperty("translations").GetArrayLength() == 2
            && exercise.GetProperty("muscle").ValueKind == JsonValueKind.String
            && exercise.GetProperty("bodyPart").ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task GetById_ShouldReturnTheExerciseToAnAnonymousCaller()
    {
        // Arrange & Act
        var response = await Client.GetAsync($"/exercises/{SeedData.SentadillaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        payload.GetProperty("id").GetGuid().Should().Be(SeedData.SentadillaId);
        payload.GetProperty("freeExerciseDbId").GetString().Should().Be("Barbell_Squat");
        payload.GetProperty("videoUrl").ValueKind.Should().Be(JsonValueKind.Null);
        payload.GetProperty("muscle").GetString().Should().Be("QUADRICEPS");
        payload.GetProperty("bodyPart").GetString().Should().Be("LEGS");
        ExerciseJson.Translations(payload).Should().BeEquivalentTo([("es", "Sentadilla con barra"), ("en", "Barbell Squat")]);
        payload.TryGetProperty("name", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetById_ShouldReturnAnEmptyTranslationListForAnExerciseWithoutNames()
    {
        // Arrange
        // The catalog is maintained in the database, so the row is stored the same way
        var id = Id.New().Value;
        await Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();
            context.Exercises.Add(new ExerciseEntity { Id = id, Muscle = (int)Muscles.NECK });
            await context.SaveChangesAsync();

            return true;
        });

        // Act
        var payload = await Client.GetFromJsonAsync<JsonElement>($"/exercises/{id}", JsonOptions);

        // Assert
        payload.GetProperty("translations").GetArrayLength().Should().Be(0);
        payload.GetProperty("bodyPart").GetString().Should().Be("NECK");
    }

    [Fact]
    public async Task Post_ShouldNotCreateAnExerciseEvenForAnAdmin()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);
        var body = new { muscle = "NECK", translations = new[] { new { languageCode = "es", name = "Nuevo ejercicio" } } };

        // Act
        var response = await admin.PostAsJsonAsync("/exercises", body);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        (await Client.GetFromJsonAsync<JsonElement>("/exercises", JsonOptions)).GetArrayLength().Should().Be(4);
    }

    [Fact]
    public async Task Put_ShouldNotUpdateAnExerciseEvenForAnAdmin()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PutAsJsonAsync($"/exercises/{SeedData.SentadillaId}", new { muscle = "GLUTES" });

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        var stored = await Client.GetFromJsonAsync<JsonElement>($"/exercises/{SeedData.SentadillaId}", JsonOptions);
        stored.GetProperty("muscle").GetString().Should().Be("QUADRICEPS");
        ExerciseJson.Translations(stored).Should().BeEquivalentTo([("es", "Sentadilla con barra"), ("en", "Barbell Squat")]);
    }

    [Fact]
    public async Task Delete_ShouldNotDeleteAnExerciseEvenForAnAdmin()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.DeleteAsync($"/exercises/{SeedData.CurlBicepsId}");

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        (await Client.GetAsync($"/exercises/{SeedData.CurlBicepsId}")).StatusCode.Should().Be(HttpStatusCode.OK);
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

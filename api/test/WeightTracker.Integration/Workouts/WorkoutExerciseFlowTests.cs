using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// End to end flows over the workout exercise routes, each ending with a read through <c>GET /workouts/{id}</c>.
/// </summary>
public class WorkoutExerciseFlowTests : WorkoutExerciseEndpointTest
{
    public WorkoutExerciseFlowTests(ApiFactory factory) : base(factory)
    { }

    private static async Task<Guid> CreateWorkout(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/workouts", new { workoutDate = "2026-11-02" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Flow_ShouldReadBackAWorkoutBuiltThroughTheExerciseRoutes()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var workoutId = await CreateWorkout(client);
        await client.PostAsJsonAsync(ExercisesRoute(workoutId), Body(SeedData.PressBancaId, "HIGH", Reps(2, 12), Reps(2, 10)));
        await client.PostAsJsonAsync(ExercisesRoute(workoutId), Body(SeedData.PlanchaId, "LOW", Sets(3, "Duration", 40)));

        // Act
        var workout = await client.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);

        // Assert
        var entries = workout.GetProperty("exercises").EnumerateArray().ToList();
        ExerciseIds(entries).Should().Equal(SeedData.PressBancaId, SeedData.PlanchaId);
        entries[0].GetProperty("effort").GetString().Should().Be("HIGH");
        SetsOf(entries[0]).Should().Equal((2, "Reps", (int?)12), (2, "Reps", (int?)10));
        entries[1].GetProperty("effort").GetString().Should().Be("LOW");
        SetsOf(entries[1]).Should().Equal((3, "Duration", (int?)40));
    }

    [Fact]
    public async Task Flow_ShouldReadBackTheLastWrittenStateAfterAnUpdateAndARemoval()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var workoutId = await CreateWorkout(client);
        await client.PostAsJsonAsync(ExercisesRoute(workoutId), Body(SeedData.PressBancaId, "HIGH", Reps(3, 10)));
        await client.PostAsJsonAsync(ExercisesRoute(workoutId), Body(SeedData.SentadillaId, "MID", Reps(4, 8)));
        await client.PostAsJsonAsync(ExercisesRoute(workoutId), Body(SeedData.PlanchaId, "LOW", Sets(3, "Duration", 40)));
        await client.PutAsJsonAsync(ExerciseRoute(workoutId, SeedData.SentadillaId), Body(SeedData.SentadillaId, "MIDLOW", Reps(2, 6), Sets(2, "MaxReps")));
        await client.DeleteAsync(ExerciseRoute(workoutId, SeedData.PressBancaId));

        // Act
        var workout = await client.GetFromJsonAsync<JsonElement>($"/workouts/{workoutId}", JsonOptions);

        // Assert
        var entries = workout.GetProperty("exercises").EnumerateArray().ToList();
        ExerciseIds(entries).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
        entries[0].GetProperty("effort").GetString().Should().Be("MIDLOW");
        SetsOf(entries[0]).Should().Equal((2, "Reps", (int?)6), (2, "MaxReps", (int?)null));
    }

    [Fact]
    public async Task Flow_ShouldAllowAnExerciseToBeAddedAgainAfterItWasRemoved()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var workoutId = await CreateWorkout(client);
        await client.PostAsJsonAsync(ExercisesRoute(workoutId), Body(SeedData.PressBancaId, "HIGH", Reps(3, 10)));
        await client.DeleteAsync(ExerciseRoute(workoutId, SeedData.PressBancaId));

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(workoutId), Body(SeedData.PressBancaId, "LOW", Reps(1, 5)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var entries = await EntriesOf(workoutId);
        entries.Should().ContainSingle();
        SetsOf(entries[0]).Should().Equal((1, "Reps", (int?)5));
    }
}

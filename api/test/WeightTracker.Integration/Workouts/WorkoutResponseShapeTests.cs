using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

public class WorkoutResponseShapeTests : IntegrationTest
{
    public WorkoutResponseShapeTests(ApiFactory factory) : base(factory)
    { }

    private async Task<JsonElement> WorkoutsSeenByAdmin()
    {
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await client.GetAsync("/workouts");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    private static JsonElement WorkoutWithId(JsonElement workouts, Guid id)
        => workouts.EnumerateArray().Single(workout => workout.GetProperty("id").GetGuid() == id);

    [Fact]
    public async Task Workout_ShouldCarryOwnershipAndDate()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("workoutDate").GetString().Should().Be("2026-10-03");
        workout.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        workout.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
    }

    [Fact]
    public async Task WorkoutWithoutTrainer_ShouldKeepAnExplicitNullTrainerId()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId);

        // Assert
        workout.TryGetProperty("trainerId", out var trainerId).Should().BeTrue();
        trainerId.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Workout_ShouldNestItsExercisesAndSets()
    {
        // Arrange
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.OwnedWorkoutId);

        var exercise = workout.GetProperty("exercises").EnumerateArray().Single();

        exercise.GetProperty("exerciseId").GetGuid().Should().Be(SeedData.PressBancaId);
        exercise.GetProperty("name").GetString().Should().Be("Press Banca");
        exercise.GetProperty("bodyPart").GetString().Should().Be("CHEST");

        // Act
        var set = exercise.GetProperty("sets").EnumerateArray().Single();

        // Assert
        set.GetProperty("count").GetInt32().Should().Be(4);
        set.GetProperty("target").GetProperty("type").GetString().Should().Be("Reps");
        set.GetProperty("target").GetProperty("value").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task DurationTarget_ShouldCarryItsValueInSeconds()
    {
        // Arrange
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        var plancha = workout.GetProperty("exercises").EnumerateArray()
            .Single(exercise => exercise.GetProperty("exerciseId").GetGuid() == SeedData.PlanchaId);

        // Act
        var target = plancha.GetProperty("sets").EnumerateArray().Single().GetProperty("target");

        // Assert
        target.GetProperty("type").GetString().Should().Be("Duration");
        target.GetProperty("value").GetInt32().Should().Be(40);
    }

    [Fact]
    public async Task MaxRepsTarget_ShouldBeSerializedWithoutAValue()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainerOwnWorkoutId);

        // Assert
        var target = workout.GetProperty("exercises").EnumerateArray().Single()
            .GetProperty("sets").EnumerateArray().Single()
            .GetProperty("target");

        target.GetProperty("type").GetString().Should().Be("MaxReps");
        target.TryGetProperty("value", out _).Should().BeFalse();
    }

    [Fact]
    public async Task WorkoutWithSeveralExercises_ShouldReturnThemAll()
    {
        // Arrange & Act
        var workout = WorkoutWithId(await WorkoutsSeenByAdmin(), SeedData.TrainedWorkoutId);

        // Assert
        workout.GetProperty("exercises").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Response_ShouldNeverLeakCredentialFields()
    {
        // Arrange & Act
        var raw = (await WorkoutsSeenByAdmin()).GetRawText();

        // Assert
        raw.Should().NotContain("passwordHash");
        raw.Should().NotContain("tokenHash");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers the role-aware scoping rule of <c>GET /workouts</c> against the seeded dataset:
/// one workout owned by the user, one owned by the user and prescribed by the trainer,
/// and one owned by the trainer themselves.
/// </summary>
public class WorkoutScopingTests : IntegrationTest
{
    public WorkoutScopingTests(ApiFactory factory) : base(factory)
    { }

    private async Task<IReadOnlyList<Guid>> WorkoutIdsVisibleTo(string email)
    {
        var client = await AuthenticatedClientFor(email);

        var response = await client.GetAsync("/workouts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        return payload.EnumerateArray()
            .Select(workout => workout.GetProperty("id").GetGuid())
            .ToList();
    }

    [Fact]
    public async Task User_ShouldOnlySeeTheirOwnWorkouts()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().BeEquivalentTo(new[] { SeedData.OwnedWorkoutId, SeedData.TrainedWorkoutId });
    }

    [Fact]
    public async Task User_ShouldNotSeeAWorkoutOwnedBySomebodyElse()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.User);

        // Assert
        visible.Should().NotContain(SeedData.TrainerOwnWorkoutId);
    }

    [Fact]
    public async Task Trainer_ShouldSeeOwnedAndPrescribedWorkouts()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.Trainer);

        // Assert
        visible.Should().BeEquivalentTo(new[] { SeedData.TrainedWorkoutId, SeedData.TrainerOwnWorkoutId });
    }

    [Fact]
    public async Task Trainer_ShouldNotSeeAWorkoutTheyNeitherOwnNorPrescribed()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.Trainer);

        // Assert
        visible.Should().NotContain(SeedData.OwnedWorkoutId);
    }

    [Fact]
    public async Task Admin_ShouldSeeEveryWorkout()
    {
        // Arrange & Act
        var visible = await WorkoutIdsVisibleTo(SeedCredentials.Admin);

        // Assert
        visible.Should().BeEquivalentTo(new[]
        {
            SeedData.OwnedWorkoutId,
            SeedData.TrainedWorkoutId,
            SeedData.TrainerOwnWorkoutId,
        });
    }
}

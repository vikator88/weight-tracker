using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Shared plumbing of the tests that need a workout in a given status. The status is set through the API
/// as an admin, who may skip the lifecycle, so no row is written directly.
/// </summary>
public abstract class WorkoutEndpointTest : IntegrationTest
{
    protected WorkoutEndpointTest(ApiFactory factory) : base(factory)
    { }

    protected async Task GivenWorkoutStatus(Guid workoutId, string status)
    {
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        var response = await admin.PutAsJsonAsync($"/workouts/{workoutId}/status", new { status });

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the workout must be arranged in '{0}' first", status);
    }
}

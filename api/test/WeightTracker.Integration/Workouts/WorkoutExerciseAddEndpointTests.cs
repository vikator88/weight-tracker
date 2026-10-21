using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>POST /workouts/{workoutId}/exercises</c>, including every branch of the workout edit rule
/// reachable with the seeded users. A second trainer on a prescribed workout is covered by unit tests.
/// </summary>
public class WorkoutExerciseAddEndpointTests : WorkoutExerciseEndpointTest
{
    public WorkoutExerciseAddEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static object ACurl() => Body(SeedData.CurlBicepsId, "HIGH", Reps(3, 15));

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectATokenWhoseUserNoLongerExists()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.TrainedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldAppendTheExerciseLastForThePrescribingTrainer()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.TrainedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().EndWith($"/workouts/{SeedData.TrainedWorkoutId}");
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        created.GetProperty("exerciseId").GetGuid().Should().Be(SeedData.CurlBicepsId);
        created.GetProperty("effort").GetString().Should().Be("HIGH");
        created.GetProperty("superSeriesId").ValueKind.Should().Be(JsonValueKind.Null);
        var entries = await EntriesOf(SeedData.TrainedWorkoutId);
        ExerciseIds(entries).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId, SeedData.CurlBicepsId);
        entries[^1].GetProperty("id").GetGuid().Should().Be(created.GetProperty("id").GetGuid());
        SetsOf(entries[^1]).Should().Equal((3, "Reps", (int?)15));
    }

    [Fact]
    public async Task Post_ShouldKeepSeveralPrescriptionsInOrder()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);
        var body = Body(SeedData.CurlBicepsId, "MID", Reps(2, 12), Reps(2, 10));

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var entries = await EntriesOf(SeedData.OwnedWorkoutId);
        SetsOf(entries[^1]).Should().Equal((2, "Reps", (int?)12), (2, "Reps", (int?)10));
    }

    [Fact]
    public async Task Post_ShouldAddTheExerciseForTheOwnerOfASelfCreatedWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseIds(await EntriesOf(SeedData.OwnedWorkoutId)).Should().Equal(SeedData.PressBancaId, SeedData.CurlBicepsId);
    }

    [Fact]
    public async Task Post_ShouldAddTheExerciseForAnAdmin()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.TrainedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Contain(SeedData.CurlBicepsId);
    }

    [Fact]
    public async Task Post_ShouldForbidTheOwnerOfAPrescribedWorkoutAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.TrainedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ExerciseIds(await EntriesOf(SeedData.TrainedWorkoutId)).Should().Equal(SeedData.SentadillaId, SeedData.PlanchaId);
    }

    [Fact]
    public async Task Post_ShouldForbidATrainerOnASelfCreatedWorkoutTheyDoNotOwnAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ExerciseIds(await EntriesOf(SeedData.OwnedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Post_ShouldForbidAUserOnAWorkoutOfSomeoneElse()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.TrainerOwnWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        ExerciseIds(await EntriesOf(SeedData.TrainerOwnWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(Guid.NewGuid()), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldReturnNotFoundForAnExerciseMissingFromTheCatalogAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), Body(Guid.NewGuid(), "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ExerciseIds(await EntriesOf(SeedData.OwnedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Post_ShouldReturnConflictForAnExerciseAlreadyInTheWorkoutAndChangeNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), Body(SeedData.PressBancaId, "MID", Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var entries = await EntriesOf(SeedData.OwnedWorkoutId);
        entries.Should().ContainSingle();
        SetsOf(entries[0]).Should().Equal((4, "Reps", (int?)10));
    }

    [Theory]
    [InlineData("MID", "MID")]
    [InlineData(1, "LOW")]
    [InlineData(5, "HIGH")]
    public async Task Post_ShouldAcceptTheEffortAsANameOrANumber(object effort, string expected)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), Body(SeedData.CurlBicepsId, effort, Reps(3, 10)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("effort").GetString().Should().Be(expected);
    }

    [Fact]
    public async Task Post_ShouldStoreMaxRepsWithoutAValueEvenWhenATargetIsSent()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), Body(SeedData.CurlBicepsId, "MID", Sets(2, "MaxReps", 7)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SetsOf((await EntriesOf(SeedData.OwnedWorkoutId))[^1]).Should().Equal((2, "MaxReps", (int?)null));
    }

    [Fact]
    public async Task Post_ShouldStoreDurationInSeconds()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), Body(SeedData.CurlBicepsId, "LOW", Sets(3, "Duration", 40)));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SetsOf((await EntriesOf(SeedData.OwnedWorkoutId))[^1]).Should().Equal((3, "Duration", (int?)40));
    }

    public static IEnumerable<object[]> InvalidBodies()
    {
        var curl = SeedData.CurlBicepsId;

        yield return ["empty sets", Body(curl, "MID")];
        yield return ["missing effort", new { exerciseId = curl, sets = new[] { Reps(3, 10) } }];
        yield return ["missing sets", new { exerciseId = curl, effort = "MID" }];
        yield return ["missing exercise id", new { effort = "MID", sets = new[] { Reps(3, 10) } }];
        yield return ["undefined effort number", Body(curl, 9, Reps(3, 10))];
        yield return ["unknown effort name", Body(curl, "EXTREME", Reps(3, 10))];
        yield return ["unknown target type", Body(curl, "MID", Sets(3, "Weight", 10))];
        yield return ["count zero", Body(curl, "MID", Reps(0, 10))];
        yield return ["reps target zero", Body(curl, "MID", Reps(3, 0))];
        yield return ["duration target zero", Body(curl, "MID", Sets(3, "Duration", 0))];
        yield return ["reps without target", Body(curl, "MID", Sets(3, "Reps"))];
    }

    [Theory]
    [MemberData(nameof(InvalidBodies))]
    public async Task Post_ShouldRejectAMalformedBodyAndChangeNothing(string scenario, object body)
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, scenario);
        ExerciseIds(await EntriesOf(SeedData.OwnedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Post_ShouldRejectThePrescribingTrainerOnAStartedWorkoutWithConflict()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute(SeedData.PrescribedStartedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIds(await EntriesOf(SeedData.PrescribedStartedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Post_ShouldRejectTheOwnerOfAFinishedWorkoutWithoutTrainerWithConflict()
    {
        // Arrange
        await GivenWorkoutStatus(SeedData.OwnedWorkoutId, "FINISHED");
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsJsonAsync(ExercisesRoute(SeedData.OwnedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ExerciseIds(await EntriesOf(SeedData.OwnedWorkoutId)).Should().Equal(SeedData.PressBancaId);
    }

    [Fact]
    public async Task Post_ShouldRejectACallerWhoMayNotEditAStartedWorkoutWithForbidden()
    {
        // Arrange
        var user = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await user.PostAsJsonAsync(ExercisesRoute(SeedData.PrescribedStartedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldAllowAnAdminOnAStartedWorkout()
    {
        // Arrange
        var admin = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await admin.PostAsJsonAsync(ExercisesRoute(SeedData.PrescribedStartedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ExerciseIds(await EntriesOf(SeedData.PrescribedStartedWorkoutId)).Should().Equal(SeedData.PressBancaId, SeedData.CurlBicepsId);
    }

    [Fact]
    public async Task Post_ShouldKeepAllowingThePrescribingTrainerOnAPublishedWorkout()
    {
        // Arrange
        var trainer = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await trainer.PostAsJsonAsync(ExercisesRoute(SeedData.TrainedWorkoutId), ACurl());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await WorkoutAsAdmin(SeedData.TrainedWorkoutId)).GetProperty("status").GetString().Should().Be("PUBLISHED");
    }
}

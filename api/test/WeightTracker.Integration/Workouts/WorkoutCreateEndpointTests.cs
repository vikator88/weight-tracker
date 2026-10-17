using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Workouts;

/// <summary>
/// Covers <c>POST /workouts</c> for every role, using only the seeded users. Each creation is checked
/// through <c>GET /workouts</c>, and each rejection must leave the set of workouts unchanged.
/// </summary>
public class WorkoutCreateEndpointTests : IntegrationTest
{
    private const string Date = "2026-10-20";

    public WorkoutCreateEndpointTests(ApiFactory factory) : base(factory)
    { }

    private async Task<IReadOnlySet<Guid>> WorkoutIdsSeenBy(string email)
    {
        var client = await AuthenticatedClientFor(email);
        var workouts = await client.GetFromJsonAsync<JsonElement>("/workouts", JsonOptions);

        return workouts.EnumerateArray().Select(workout => workout.GetProperty("id").GetGuid()).ToHashSet();
    }

    private static async Task<JsonElement> BodyOf(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

    public static TheoryData<string, object, HttpStatusCode> Rejections => new()
    {
        // USER
        { SeedCredentials.User, new { userId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { trainerId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        // TRAINER
        { SeedCredentials.Trainer, new { workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Trainer, new { userId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Trainer, new { userId = SeedData.UserId, trainerId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Trainer, new { userId = SeedData.AdminId, workoutDate = Date }, HttpStatusCode.Forbidden },
        { SeedCredentials.Trainer, new { userId = Guid.NewGuid(), workoutDate = Date }, HttpStatusCode.NotFound },
        // ADMIN
        { SeedCredentials.Admin, new { workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { userId = SeedData.AdminId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { userId = SeedData.UserId, trainerId = SeedData.UserId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { userId = SeedData.TrainerId, trainerId = SeedData.TrainerId, workoutDate = Date }, HttpStatusCode.BadRequest },
        { SeedCredentials.Admin, new { userId = Guid.NewGuid(), workoutDate = Date }, HttpStatusCode.NotFound },
        // STRUCTURAL
        { SeedCredentials.User, new { }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { workoutDate = "not-a-date" }, HttpStatusCode.BadRequest },
        { SeedCredentials.User, new { workoutDate = "2026-10-20T10:30:00" }, HttpStatusCode.BadRequest },
    };

    [Fact]
    public async Task Post_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.PostAsJsonAsync("/workouts", new { workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.PostAsJsonAsync("/workouts", new { workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldCreateTheWorkoutForTheCallerWhenAUserSendsOnlyTheDate()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("workoutDate").GetString().Should().Be(Date);
        body.GetProperty("exercises").GetArrayLength().Should().Be(0);
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().Contain(body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ShouldAcceptTheCallersOwnIdWhenAUserSendsIt()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.User);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await BodyOf(response)).GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
    }

    [Fact]
    public async Task Post_ShouldPrescribeTheWorkoutWhenATrainerCreatesItForAUser()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        var id = body.GetProperty("id").GetGuid();
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().Contain(id);
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().Contain(id);
    }

    [Fact]
    public async Task Post_ShouldCreateTheWorkoutWithoutTrainerWhenAnAdminSendsNone()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", new { userId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        var id = body.GetProperty("id").GetGuid();
        body.GetProperty("userId").GetGuid().Should().Be(SeedData.UserId);
        body.GetProperty("trainerId").ValueKind.Should().Be(JsonValueKind.Null);
        (await WorkoutIdsSeenBy(SeedCredentials.User)).Should().Contain(id);
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().NotContain(id);
    }

    [Fact]
    public async Task Post_ShouldAssignTheTrainerWhenAnAdminSendsOne()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts",
            new { userId = SeedData.UserId, trainerId = SeedData.TrainerId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await BodyOf(response);
        body.GetProperty("trainerId").GetGuid().Should().Be(SeedData.TrainerId);
        (await WorkoutIdsSeenBy(SeedCredentials.Trainer)).Should().Contain(body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Post_ShouldRejectAnAdminNamingAUserAsTrainer()
    {
        // Arrange
        var before = await WorkoutIdsSeenBy(SeedCredentials.Admin);
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.PostAsJsonAsync("/workouts",
            new { userId = SeedData.TrainerId, trainerId = SeedData.UserId, workoutDate = Date });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await WorkoutIdsSeenBy(SeedCredentials.Admin)).Should().BeEquivalentTo(before);
    }

    [Theory]
    [MemberData(nameof(Rejections))]
    public async Task Post_ShouldRejectTheRequestAndCreateNothing(string caller, object body, HttpStatusCode expected)
    {
        // Arrange
        var before = await WorkoutIdsSeenBy(SeedCredentials.Admin);
        var client = await AuthenticatedClientFor(caller);

        // Act
        var response = await client.PostAsJsonAsync("/workouts", body);

        // Assert
        response.StatusCode.Should().Be(expected);
        (await WorkoutIdsSeenBy(SeedCredentials.Admin)).Should().BeEquivalentTo(before);
    }
}

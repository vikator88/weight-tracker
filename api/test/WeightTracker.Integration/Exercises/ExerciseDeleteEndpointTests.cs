using System.Net;
using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Exercises;

public class ExerciseDeleteEndpointTests : IntegrationTest
{
    public ExerciseDeleteEndpointTests(ApiFactory factory) : base(factory)
    { }

    private static string Route(Guid id) => $"/exercises/{id}";

    [Fact]
    public async Task Delete_ShouldRejectAnonymousCallers()
    {
        // Arrange & Act
        var response = await Client.DeleteAsync(Route(SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRejectAGarbageToken()
    {
        // Arrange
        Client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        // Act
        var response = await Client.DeleteAsync(Route(SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldRejectATokenWhoseAdminNoLongerExistsAndDeleteNothing()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);
        await Factory.RemoveUser(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var stored = await Client.GetAsync(Route(SeedData.CurlBicepsId));
        stored.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_ShouldRejectATokenWhoseNonAdminUserNoLongerExistsAsUnauthorizedNotForbidden()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Trainer);
        await Factory.RemoveUser(SeedCredentials.Trainer);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(SeedCredentials.User)]
    [InlineData(SeedCredentials.Trainer)]
    public async Task Delete_ShouldForbidNonAdminCallersAndRemoveNothing(string email)
    {
        // Arrange
        var client = await AuthenticatedClientFor(email);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetAsync(Route(SeedData.CurlBicepsId))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_ShouldRemoveAnExerciseNoWorkoutUses()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.CurlBicepsId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Client.GetAsync(Route(SeedData.CurlBicepsId))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundForAnUnknownExercise()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(Route(Id.New().Value));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldConflictOnAnExerciseIncludedInAWorkoutAndKeepIt()
    {
        // Arrange
        var client = await AuthenticatedClientFor(SeedCredentials.Admin);

        // Act
        var response = await client.DeleteAsync(Route(SeedData.PressBancaId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Client.GetAsync(Route(SeedData.PressBancaId))).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

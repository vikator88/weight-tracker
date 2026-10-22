using System.Net;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Cors;

/// <summary>
/// Guards that the allow-all CORS policy stays Development only.
/// </summary>
/// <remarks>
/// The policy is applied inside the Development block of <c>Program.cs</c>. The integration host
/// runs as Testing, so it can only prove the policy does not leak into other environments.
/// </remarks>
public class CorsPolicyTests : IntegrationTest
{
    public CorsPolicyTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task CrossOriginRequest_ShouldNotReceiveCorsHeadersOutsideDevelopment()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/exercises");
        request.Headers.Add("Origin", "http://localhost:8081");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}

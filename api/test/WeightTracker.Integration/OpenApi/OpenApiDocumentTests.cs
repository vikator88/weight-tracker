using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.OpenApi;

/// <summary>
/// Guards the OpenAPI document transformers.
/// </summary>
/// <remarks>
/// The bearer scheme and the per-operation security requirements are contributed by custom
/// transformers. If either stops working the build still succeeds and the API still serves
/// traffic, but the reference UI silently loses its authentication box, so the contract is
/// asserted here instead.
/// </remarks>
public class OpenApiDocumentTests : IntegrationTest
{
    private const string DocumentPath = "/openapi/v1.json";

    public OpenApiDocumentTests(ApiFactory factory) : base(factory)
    { }

    private async Task<JsonElement> Document()
    {
        var response = await Client.GetAsync(DocumentPath);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    private static JsonElement Operation(JsonElement document, string path, string method)
        => document.GetProperty("paths").GetProperty(path).GetProperty(method);

    [Fact]
    public async Task Document_ShouldBeServed()
    {
        // Arrange & Act
        var response = await Client.GetAsync(DocumentPath);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Document_ShouldDeclareTheBearerSecurityScheme()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var scheme = document
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        scheme.GetProperty("type").GetString().Should().Be("http");
        scheme.GetProperty("scheme").GetString().Should().Be("bearer");
        scheme.GetProperty("bearerFormat").GetString().Should().Be("JWT");
    }

    [Fact]
    public async Task ProtectedOperation_ShouldCarryAResolvedSecurityRequirement()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var security = Operation(document, "/workouts", "get").GetProperty("security");

        security.GetArrayLength().Should().Be(1);
        security[0].TryGetProperty("Bearer", out var scopes)
            .Should().BeTrue("the requirement must reference the declared scheme, not serialize empty");
        scopes.GetArrayLength().Should().Be(0);
    }

    [Theory]
    [InlineData("/auth/login")]
    [InlineData("/auth/refresh")]
    public async Task AnonymousOperation_ShouldCarryNoSecurityRequirement(string path)
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        Operation(document, path, "post").TryGetProperty("security", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Workouts_ShouldDocumentItsResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/workouts", "get").GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "401", "403"]);
    }

    [Fact]
    public async Task Login_ShouldDocumentItsResponses()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var responses = Operation(document, "/auth/login", "post").GetProperty("responses");

        responses.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "400", "401"]);
    }

    [Fact]
    public async Task SuccessfulLogin_ShouldDocumentTheTokenFieldNames()
    {
        // Arrange & Act
        var document = await Document();

        // Assert
        var schemaName = Operation(document, "/auth/login", "post")
            .GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString()!
            .Split('/')[^1];

        var properties = document
            .GetProperty("components").GetProperty("schemas").GetProperty(schemaName)
            .GetProperty("properties");

        properties.EnumerateObject().Select(property => property.Name)
            .Should().Contain(["accessToken", "refreshToken", "expiresIn"]);
    }

    [Fact]
    public async Task ReferenceUi_ShouldNotBeServedOutsideDevelopment()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/scalar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

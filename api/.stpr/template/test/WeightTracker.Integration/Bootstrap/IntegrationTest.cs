using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace WeightTracker.Integration.Bootstrap;

[Collection(ApiCollection.Name)]
public abstract class IntegrationTest : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    protected ApiFactory Factory { get; }

    protected HttpClient Client { get; }

    protected IntegrationTest(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public Task InitializeAsync() => Factory.ResetDatabase();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Logs a seeded user in and returns the raw login payload.
    /// </summary>
    protected async Task<JsonElement> Login(string email, string password = SeedCredentials.Password)
    {
        var response = await Client.PostAsJsonAsync("/auth/login", new { email, password });

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
    }

    /// <summary>
    /// Returns an HTTP client already carrying the bearer token of the given seeded user.
    /// </summary>
    protected async Task<HttpClient> AuthenticatedClientFor(string email)
    {
        var payload = await Login(email);
        var client = Factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload.GetProperty("accessToken").GetString());

        return client;
    }
}

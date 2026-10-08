using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Migrations;
using Xunit;

namespace WeightTracker.Integration.Bootstrap;

/// <summary>
/// Hosts the API in process against a real Postgres database.
/// </summary>
/// <remarks>
/// Runs against <c>weight-tracker-test</c> on the container from
/// <c>infra/docker-compose.yml</c>, deliberately a different database from the one used for
/// local development, so running the suite never wipes development data.
/// </remarks>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "weight-tracker-test";

    public const string ConnectionString =
        $"Host=localhost;Port=5432;Database={DatabaseName};Username=admin;Password=admin";

    private const string MaintenanceConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin";

    private const string SigningKey = "integration-test-signing-key-at-least-32-bytes";

    static ApiFactory()
    {
        // The API validates its configuration at the top of Program, before the host is
        // built, so the overrides have to be in place through the environment variable
        // provider rather than through ConfigureAppConfiguration.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: the fixture owns migrating and seeding, not application startup
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Jwt:SigningKey"] = SigningKey,
            }));
    }

    public async Task InitializeAsync()
    {
        // The compose file only provisions the development database, so the suite creates
        // its own. Keeps `docker compose up` plus `dotnet test` enough on a fresh clone.
        await EnsureDatabaseExists();

        DatabaseMigrator.MigrateUp(ConnectionString);
    }

    private static async Task EnsureDatabaseExists()
    {
        await using var connection = new NpgsqlConnection(MaintenanceConnectionString);
        await connection.OpenAsync();

        await using var exists = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", DatabaseName);

        if (await exists.ExecuteScalarAsync() is not null)
            return;

        await using var create = new NpgsqlCommand($"""CREATE DATABASE "{DatabaseName}" """, connection);
        await create.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Runs an action against a fresh dependency injection scope, so repository tests use
    /// the same wiring the API uses.
    /// </summary>
    public async Task<TResult> WithScope<TResult>(Func<IServiceProvider, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();

        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Empties every table and reapplies the seeded dataset, so each test starts from the
    /// same known state.
    /// </summary>
    public async Task ResetDatabase()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<WeightTrackerDbContext>();

        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                refresh_tokens,
                workout_exercise_sets,
                workout_exercises,
                workouts,
                users,
                exercises
            RESTART IDENTITY CASCADE
            """);

        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().Seed(CancellationToken.None);
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

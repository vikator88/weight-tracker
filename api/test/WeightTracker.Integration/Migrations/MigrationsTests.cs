using FluentAssertions;
using Npgsql;
using WeightTracker.Migrations;
using Xunit;

namespace WeightTracker.Integration.Migrations;

/// <summary>
/// Verifies the schema applies from scratch and reverses cleanly.
/// </summary>
/// <remarks>
/// Uses a throwaway database of its own so it never disturbs the database the rest of the
/// suite shares.
/// </remarks>
public class MigrationsTests : IAsyncLifetime
{
    private const string MaintenanceConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin";

    private const string DatabaseName = "weight-tracker-migrations-test";

    // Pooling is off because dropping the database between tests terminates pooled
    // connections, which would otherwise poison the pool for the next test.
    private static readonly string ConnectionString =
        $"Host=localhost;Port=5432;Database={DatabaseName};Username=admin;Password=admin;Pooling=false";

    private static readonly string[] ExpectedTables =
    [
        "users",
        "exercises",
        "workouts",
        "workout_exercises",
        "workout_exercise_sets",
        "refresh_tokens",
    ];

    public async Task InitializeAsync()
    {
        await DropDatabase();
        await Execute(MaintenanceConnectionString, $"""CREATE DATABASE "{DatabaseName}" """);
    }

    public Task DisposeAsync() => DropDatabase();

    [Fact]
    public void MigrateUp_ShouldCreateEveryTableFromAnEmptyDatabase()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(ConnectionString);

        // Assert
        foreach (var table in ExpectedTables)
            TableExists(table).Should().BeTrue($"migration should have created '{table}'");
    }

    [Fact]
    public void MigrateUp_ShouldBeIdempotentWhenRunTwice()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(ConnectionString);

        // Act
        var act = () => DatabaseMigrator.MigrateUp(ConnectionString);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MigrateDown_ShouldRemoveEveryTable()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(ConnectionString);

        // Act
        DatabaseMigrator.MigrateDown(ConnectionString);

        // Assert
        foreach (var table in ExpectedTables)
            TableExists(table).Should().BeFalse($"rollback should have dropped '{table}'");
    }

    private static bool TableExists(string table)
    {
        using var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT to_regclass(@table) IS NOT NULL", connection);
        command.Parameters.AddWithValue("table", $"public.{table}");

        return (bool)command.ExecuteScalar()!;
    }

    private static async Task DropDatabase()
    {
        NpgsqlConnection.ClearAllPools();

        await Execute(
            MaintenanceConnectionString,
            $"""DROP DATABASE IF EXISTS "{DatabaseName}" WITH (FORCE)""");
    }

    private static async Task Execute(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

using FluentAssertions;
using Npgsql;
using WeightTracker.Integration.Bootstrap;
using WeightTracker.Migrations;
using Xunit;

namespace WeightTracker.Integration.Migrations;

/// <summary>
/// Verifies the schema applies from scratch and reverses cleanly.
/// </summary>
/// <remarks>
/// Uses a throwaway database of its own on the suite's container, so it never disturbs the
/// database the rest of the suite shares.
/// </remarks>
[Collection(ApiCollection.Name)]
public class MigrationsTests : IAsyncLifetime
{
    private const string DatabaseName = "weight-tracker-migrations-test";

    private static readonly string[] ExpectedTables =
    [
        "users",
        "exercises",
        "workouts",
        "workout_exercises",
        "workout_exercise_sets",
        "refresh_tokens",
    ];

    private readonly string _maintenanceConnectionString;

    private readonly string _connectionString;

    public MigrationsTests(ApiFactory factory)
    {
        _maintenanceConnectionString = factory.ConnectionStringFor("postgres");

        // Pooling is off because dropping the database between tests terminates pooled
        // connections, which would otherwise poison the pool for the next test.
        _connectionString = factory.ConnectionStringFor(DatabaseName, pooling: false);
    }

    public async Task InitializeAsync()
    {
        await DropDatabase();
        await Execute(_maintenanceConnectionString, $"""CREATE DATABASE "{DatabaseName}" """);
    }

    public Task DisposeAsync() => DropDatabase();

    [Fact]
    public void MigrateUp_ShouldCreateEveryTableFromAnEmptyDatabase()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString);

        // Assert
        foreach (var table in ExpectedTables)
            TableExists(table).Should().BeTrue($"migration should have created '{table}'");
    }

    [Fact]
    public void MigrateUp_ShouldBeIdempotentWhenRunTwice()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString);

        // Act
        var act = () => DatabaseMigrator.MigrateUp(_connectionString);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MigrateDown_ShouldRemoveEveryTable()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString);

        // Assert
        foreach (var table in ExpectedTables)
            TableExists(table).Should().BeFalse($"rollback should have dropped '{table}'");
    }

    private bool TableExists(string table)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT to_regclass(@table) IS NOT NULL", connection);
        command.Parameters.AddWithValue("table", $"public.{table}");

        return (bool)command.ExecuteScalar()!;
    }

    private async Task DropDatabase()
    {
        NpgsqlConnection.ClearAllPools();

        await Execute(
            _maintenanceConnectionString,
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

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

    /// <summary>Last migration before <c>AddEffortToWorkoutExercises_202610131437</c> that it depends on.</summary>
    private const long BeforeEffortVersion = 202610080944;

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

    [Fact]
    public async Task AddEffort_ShouldBackfillExistingRowsWithMid()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, BeforeEffortVersion);
        await Execute(_connectionString, """
            INSERT INTO users (id, email, name, surname, date_birth, role, password_hash, created_at)
            VALUES ('00000000-0000-0000-0000-0000000000a1', 'legacy@weighttracker.test', 'Legacy', 'User',
                    '1990-01-01', 1, 'hash', now());
            INSERT INTO exercises (id, name, video_url, body_part)
            VALUES ('00000000-0000-0000-0000-0000000000a2', 'Press Banca', NULL, 3);
            INSERT INTO workouts (id, user_id, trainer_id, workout_date)
            VALUES ('00000000-0000-0000-0000-0000000000a3', '00000000-0000-0000-0000-0000000000a1', NULL, '2026-10-01');
            INSERT INTO workout_exercises (id, workout_id, exercise_id, position)
            VALUES ('00000000-0000-0000-0000-0000000000a4', '00000000-0000-0000-0000-0000000000a3',
                    '00000000-0000-0000-0000-0000000000a2', 0);
            """);

        // Act
        DatabaseMigrator.MigrateUp(_connectionString);

        // Assert
        (await Scalar<int>("SELECT effort FROM workout_exercises")).Should().Be(3);
    }

    [Fact]
    public async Task AddEffort_ShouldLeaveNoDefaultForNewRows()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString);

        // Assert
        (await Scalar<bool>("""
            SELECT column_default IS NULL FROM information_schema.columns
            WHERE table_name = 'workout_exercises' AND column_name = 'effort'
            """)).Should().BeTrue();
    }

    [Fact]
    public async Task AddEffort_Down_ShouldRemoveTheColumn()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeEffortVersion);

        // Assert
        (await Scalar<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_name = 'workout_exercises' AND column_name = 'effort')
            """)).Should().BeFalse();
    }

    private async Task<T> Scalar<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        return (T)(await command.ExecuteScalarAsync())!;
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

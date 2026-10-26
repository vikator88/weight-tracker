using FluentAssertions;
using Npgsql;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Services;
using WeightTracker.Integration.Bootstrap;
using WeightTracker.Migrations;
using WeightTracker.Migrations.Migrations;
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

    /// <summary>Last migration before <c>InsertAdminUser_202610080919</c> that it depends on.</summary>
    private const long BeforeAdminUserVersion = 202610080917;

    /// <summary>Last migration before <c>AddEffortToWorkoutExercises_202610131437</c> that it depends on.</summary>
    private const long BeforeEffortVersion = 202610080944;

    /// <summary>Last migration before <c>AddStatusToWorkouts_202610182247</c> that it depends on.</summary>
    private const long BeforeStatusVersion = 202610161647;

    private static readonly string[] ExpectedTables =
    [
        "Users",
        "Exercises",
        "Workouts",
        "WorkoutExercises",
        "WorkoutExerciseSets",
        "RefreshTokens",
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
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        foreach (var table in ExpectedTables)
            TableExists(table).Should().BeTrue($"migration should have created '{table}'");
    }

    [Fact]
    public void MigrateUp_ShouldBeIdempotentWhenRunTwice()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        var act = () => DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MigrateDown_ShouldRemoveEveryTable()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

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
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser, BeforeEffortVersion);
        await Execute(_connectionString, """
            INSERT INTO "Users" ("Id", "Email", "Name", "Surname", "DateBirth", "Role", "PasswordHash", "CreatedAt")
            VALUES ('00000000-0000-0000-0000-0000000000a1', 'legacy@weighttracker.test', 'Legacy', 'User',
                    '1990-01-01', 1, 'hash', now());
            INSERT INTO "Exercises" ("Id", "Name", "VideoUrl", "BodyPart")
            VALUES ('00000000-0000-0000-0000-0000000000a2', 'Press Banca', NULL, 3);
            INSERT INTO "Workouts" ("Id", "UserId", "TrainerId", "WorkoutDate")
            VALUES ('00000000-0000-0000-0000-0000000000a3', '00000000-0000-0000-0000-0000000000a1', NULL, '2026-10-01');
            INSERT INTO "WorkoutExercises" ("Id", "WorkoutId", "ExerciseId", "Position")
            VALUES ('00000000-0000-0000-0000-0000000000a4', '00000000-0000-0000-0000-0000000000a3',
                    '00000000-0000-0000-0000-0000000000a2', 0);
            """);

        // Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<int>("SELECT \"Effort\" FROM \"WorkoutExercises\"")).Should().Be(3);
    }

    [Fact]
    public async Task AddEffort_ShouldLeaveNoDefaultForNewRows()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<bool>("""
            SELECT column_default IS NULL FROM information_schema.columns
            WHERE table_name = 'WorkoutExercises' AND column_name = 'Effort'
            """)).Should().BeTrue();
    }

    [Fact]
    public async Task AddEffort_Down_ShouldRemoveTheColumn()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeEffortVersion);

        // Assert
        (await Scalar<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_name = 'WorkoutExercises' AND column_name = 'Effort')
            """)).Should().BeFalse();
    }

    [Fact]
    public async Task AddStatus_ShouldBackfillExistingRowsWithDraft()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser, BeforeStatusVersion);
        await Execute(_connectionString, """
            INSERT INTO "Users" ("Id", "Email", "Name", "Surname", "DateBirth", "Role", "PasswordHash", "CreatedAt")
            VALUES ('00000000-0000-0000-0000-0000000000b1', 'legacy-status@weighttracker.test', 'Legacy', 'User',
                    '1990-01-01', 1, 'hash', now());
            INSERT INTO "Workouts" ("Id", "UserId", "TrainerId", "WorkoutDate")
            VALUES ('00000000-0000-0000-0000-0000000000b2', '00000000-0000-0000-0000-0000000000b1', NULL, '2026-10-01');
            """);

        // Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<int>("SELECT \"Status\" FROM \"Workouts\"")).Should().Be(1);
    }

    [Fact]
    public async Task AddStatus_ShouldLeaveNoDefaultForNewRows()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<bool>("""
            SELECT column_default IS NULL FROM information_schema.columns
            WHERE table_name = 'Workouts' AND column_name = 'Status'
            """)).Should().BeTrue();
    }

    [Fact]
    public async Task AddStatus_Down_ShouldRemoveTheColumn()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeStatusVersion);

        // Assert
        (await Scalar<bool>("""
            SELECT EXISTS (SELECT 1 FROM information_schema.columns
            WHERE table_name = 'Workouts' AND column_name = 'Status')
            """)).Should().BeFalse();
    }

    [Fact]
    public async Task InsertAdminUser_ShouldInsertTheConfiguredAdmin()
    {
        // Arrange & Act
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Assert
        (await Scalar<string>(AdminColumn("Email"))).Should().Be(ApiFactory.AdminUser.Email);
        (await Scalar<int>(AdminColumn("Role"))).Should().Be((int)Role.ADMIN);
        new PasswordHasher().Verify(ApiFactory.AdminUser.Password, PasswordHash.From(await Scalar<string>(AdminColumn("PasswordHash"))))
            .Should().BeTrue("login must be able to verify the stored hash");
    }

    [Fact]
    public void InsertAdminUser_ShouldFailWithoutConfiguredCredentials()
    {
        // Arrange
        var missingCredentials = new AdminUserOptions();

        // Act
        var act = () => DatabaseMigrator.MigrateUp(_connectionString, missingCredentials);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*AdminUser:Email*");
    }

    [Fact]
    public async Task InsertAdminUser_Down_ShouldRemoveTheAdmin()
    {
        // Arrange
        DatabaseMigrator.MigrateUp(_connectionString, ApiFactory.AdminUser);

        // Act
        DatabaseMigrator.MigrateDown(_connectionString, BeforeAdminUserVersion);

        // Assert
        (await Scalar<long>("SELECT count(*) FROM \"Users\"")).Should().Be(0);
    }

    private static string AdminColumn(string column) =>
        $"SELECT \"{column}\" FROM \"Users\" WHERE \"Id\" = '{InsertAdminUser_202610080919.AdminId}'";

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
        command.Parameters.AddWithValue("table", $"public.\"{table}\"");

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

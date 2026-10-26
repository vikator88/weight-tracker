using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace WeightTracker.Migrations;

/// <summary>
/// Applies the FluentMigrator schema to a Postgres database.
/// </summary>
/// <remarks>
/// FluentMigrator is the only schema authority of the project. Entity Framework maps
/// onto the resulting tables and must never generate or alter them.
/// </remarks>
public static class DatabaseMigrator
{
    public static void MigrateUp(string connectionString, AdminUserOptions adminUser)
    {
        using var serviceProvider = BuildServiceProvider(connectionString, adminUser);
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    /// <summary>
    /// Applies migrations up to and including <paramref name="targetVersion"/>. Used to verify
    /// how a migration treats rows that already exist.
    /// </summary>
    public static void MigrateUp(string connectionString, AdminUserOptions adminUser, long targetVersion)
    {
        using var serviceProvider = BuildServiceProvider(connectionString, adminUser);
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp(targetVersion);
    }

    /// <summary>
    /// Rolls migrations back down to <paramref name="targetVersion"/>; every migration by default.
    /// Used by the migration round-trip verification.
    /// </summary>
    public static void MigrateDown(string connectionString, long targetVersion = 0)
    {
        // Rolling back never reads the admin credentials: the admin row is removed by its fixed id
        using var serviceProvider = BuildServiceProvider(connectionString, new AdminUserOptions());
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateDown(targetVersion);
    }

    private static ServiceProvider BuildServiceProvider(string connectionString, AdminUserOptions adminUser)
    {
        return new ServiceCollection()
            .AddSingleton(adminUser)
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(DatabaseMigrator).Assembly).For.Migrations())
            .AddLogging(logging => logging.AddFluentMigratorConsole())
            .BuildServiceProvider(validateScopes: false);
    }
}

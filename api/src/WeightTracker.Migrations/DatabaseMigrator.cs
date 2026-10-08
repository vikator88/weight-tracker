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
    public static void MigrateUp(string connectionString)
    {
        using var serviceProvider = BuildServiceProvider(connectionString);
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    /// <summary>
    /// Rolls every migration back. Used by the migration round-trip verification.
    /// </summary>
    public static void MigrateDown(string connectionString)
    {
        using var serviceProvider = BuildServiceProvider(connectionString);
        using var scope = serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateDown(0);
    }

    private static ServiceProvider BuildServiceProvider(string connectionString)
    {
        return new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(DatabaseMigrator).Assembly).For.Migrations())
            .AddLogging(logging => logging.AddFluentMigratorConsole())
            .BuildServiceProvider(validateScopes: false);
    }
}

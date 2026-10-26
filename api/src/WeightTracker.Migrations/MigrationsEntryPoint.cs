namespace WeightTracker.Migrations;

/// <summary>
/// Command line entry point for applying the schema, so migrations can run without
/// starting the API. The API still applies them itself on startup in Development.
/// The admin credentials come from the AdminUser__Email and AdminUser__Password environment variables.
/// </summary>
internal static class MigrationsEntryPoint
{
    private const string ConnectionStringVariable = "ConnectionStrings__Default";

    private const string AdminEmailVariable = "AdminUser__Email";

    private const string AdminPasswordVariable = "AdminUser__Password";

    private static int Main(string[] args)
    {
        var connectionString = args.FirstOrDefault(argument => argument.StartsWith("--") == false)
            ?? Environment.GetEnvironmentVariable(ConnectionStringVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine(
                $"No connection string. Pass it as the first argument or set {ConnectionStringVariable}.");

            return 1;
        }

        try
        {
            if (args.Contains("--down"))
                DatabaseMigrator.MigrateDown(connectionString);
            else
                DatabaseMigrator.MigrateUp(connectionString, new AdminUserOptions
                {
                    Email = Environment.GetEnvironmentVariable(AdminEmailVariable) ?? string.Empty,
                    Password = Environment.GetEnvironmentVariable(AdminPasswordVariable) ?? string.Empty,
                });

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Migration failed: {exception.Message}");

            return 1;
        }
    }
}

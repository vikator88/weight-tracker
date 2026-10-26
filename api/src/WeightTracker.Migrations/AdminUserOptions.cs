namespace WeightTracker.Migrations;

/// <summary>
/// Bound from the <c>AdminUser</c> section of the application configuration. Credentials of the
/// administrator inserted by the migrations.
/// </summary>
public class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    /// <summary>Never committed outside the development configuration.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Never committed outside the development configuration.</summary>
    public string Password { get; set; } = string.Empty;
}

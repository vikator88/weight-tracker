using FluentMigrator;
using Microsoft.AspNetCore.Identity;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080919)]
public sealed class InsertAdminUser_202610080919 : Migration
{
    /// <summary>Fixed so Down() can remove the row without knowing the configured email.</summary>
    public static readonly Guid AdminId = new("c8886cd1-5d59-4300-a1bd-007839cdc17c");

    // Role.ADMIN. The migrations project references no other project, so the enum is not reachable
    private const int AdminRole = 3;

    private readonly AdminUserOptions _adminUser;

    public InsertAdminUser_202610080919(AdminUserOptions adminUser)
    {
        _adminUser = adminUser;
    }

    public override void Up()
    {
        if (string.IsNullOrWhiteSpace(_adminUser.Email) || string.IsNullOrWhiteSpace(_adminUser.Password))
            throw new InvalidOperationException(
                $"{AdminUserOptions.SectionName}:Email and {AdminUserOptions.SectionName}:Password must be configured. "
                    + "Supply them through configuration or the AdminUser__Email and AdminUser__Password environment variables.");

        // Same hasher and format as Infra's PasswordHasher, so login verifies this row
        var passwordHash = new PasswordHasher<object>().HashPassword(new object(), _adminUser.Password);

        Insert.IntoTable("Users").Row(new
        {
            Id = AdminId,
            // Stored normalized, as the Email value object does
            Email = _adminUser.Email.Trim().ToLowerInvariant(),
            Name = "Admin",
            Surname = "Admin",
            DateBirth = new DateTime(1990, 1, 1),
            Role = AdminRole,
            PasswordHash = passwordHash,
            CreatedAt = RawSql.Insert("now()"),
        });
    }

    public override void Down()
    {
        Delete.FromTable("Users").Row(new { Id = AdminId });
    }
}

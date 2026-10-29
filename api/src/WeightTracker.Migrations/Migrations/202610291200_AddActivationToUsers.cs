using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Adds the activation state of a user: whether the account is active and, when it is not, since when.
/// </summary>
[Migration(202610291200)]
public sealed class AddActivationToUsers_202610291200 : Migration
{
    public override void Up()
    {
        // The default only backfills the rows that already exist, the admin included, as active
        Alter.Table("Users")
            .AddColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
            .AddColumn("DeactivatedAtUtc").AsCustom("timestamptz").Nullable();

        // Dropped afterwards so the database never invents an activation state for a new row
        Delete.DefaultConstraint().OnTable("Users").OnColumn("IsActive");
    }

    public override void Down()
    {
        Delete.Column("DeactivatedAtUtc").FromTable("Users");
        Delete.Column("IsActive").FromTable("Users");
    }
}

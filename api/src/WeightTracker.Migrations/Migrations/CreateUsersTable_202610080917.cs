using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080917)]
public sealed class CreateUsersTable_202610080917 : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_users")
            // Length aligned with Email.MaxLength
            .WithColumn("email").AsString(254).NotNullable()
            // Length aligned with PersonName.MaxLength
            .WithColumn("name").AsString(100).NotNullable()
            // Length aligned with PersonName.MaxLength
            .WithColumn("surname").AsString(100).NotNullable()
            .WithColumn("date_birth").AsDate().NotNullable()
            .WithColumn("role").AsInt32().NotNullable()
            // Length aligned with PasswordHash.MaxLength
            .WithColumn("password_hash").AsString(500).NotNullable()
            .WithColumn("created_at").AsCustom("timestamptz").NotNullable();

        // Email is a unique business identifier
        Create.UniqueConstraint("uq_users_email").OnTable("users").Column("email");
    }

    public override void Down()
    {
        Delete.Table("users");
    }
}

using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080917)]
public sealed class CreateUsersTable_202610080917 : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_Users")
            // Length aligned with Email.MaxLength
            .WithColumn("Email").AsString(254).NotNullable()
            // Length aligned with PersonName.MaxLength
            .WithColumn("Name").AsString(100).NotNullable()
            // Length aligned with PersonName.MaxLength
            .WithColumn("Surname").AsString(100).NotNullable()
            .WithColumn("DateBirth").AsDate().NotNullable()
            .WithColumn("Role").AsInt32().NotNullable()
            // Length aligned with PasswordHash.MaxLength
            .WithColumn("PasswordHash").AsString(500).NotNullable()
            .WithColumn("CreatedAt").AsCustom("timestamptz").NotNullable();

        // Email is a unique business identifier
        Create.UniqueConstraint("UQ_Users_Email").OnTable("Users").Column("Email");
    }

    public override void Down()
    {
        Delete.Table("Users");
    }
}

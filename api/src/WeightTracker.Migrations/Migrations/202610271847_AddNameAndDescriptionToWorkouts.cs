using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610271847)]
public sealed class AddNameAndDescriptionToWorkouts_202610271847 : Migration
{
    public override void Up()
    {
        // The default only backfills the rows that already exist
        Alter.Table("Workouts")
            .AddColumn("Name").AsString(256).NotNullable().WithDefaultValue("Workout")
            .AddColumn("Description").AsString(1024).Nullable();

        // Dropped afterwards so the database never invents a name for a new row
        Delete.DefaultConstraint().OnTable("Workouts").OnColumn("Name");
    }

    public override void Down()
    {
        Delete.Column("Name").FromTable("Workouts");
        Delete.Column("Description").FromTable("Workouts");
    }
}

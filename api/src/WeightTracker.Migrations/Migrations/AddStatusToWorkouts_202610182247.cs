using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610182247)]
public sealed class AddStatusToWorkouts_202610182247 : Migration
{
    public override void Up()
    {
        // 1 is WorkoutStatus.DRAFT. The default only backfills the rows that already exist
        Alter.Table("workouts")
            .AddColumn("status").AsInt32().NotNullable().WithDefaultValue(1);

        // Dropped afterwards so the database never invents a status for a new row
        Delete.DefaultConstraint().OnTable("workouts").OnColumn("status");
    }

    public override void Down()
    {
        Delete.Column("status").FromTable("workouts");
    }
}

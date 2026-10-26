using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610131437)]
public sealed class AddEffortToWorkoutExercises_202610131437 : Migration
{
    public override void Up()
    {
        // 3 is Effort.MID. The default only backfills the rows that already exist
        Alter.Table("WorkoutExercises")
            .AddColumn("Effort").AsInt32().NotNullable().WithDefaultValue(3);

        // Dropped afterwards so the database never invents an effort for a new row
        Delete.DefaultConstraint().OnTable("WorkoutExercises").OnColumn("Effort");
    }

    public override void Down()
    {
        Delete.Column("Effort").FromTable("WorkoutExercises");
    }
}

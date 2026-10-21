using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610212330)]
public sealed class AddTargetMaxValueToWorkoutExerciseSets_202610212330 : Migration
{
    public override void Up()
    {
        // Null for every target type except RepsRange, whose min stays in target_value
        Alter.Table("workout_exercise_sets")
            .AddColumn("target_max_value").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Column("target_max_value").FromTable("workout_exercise_sets");
    }
}

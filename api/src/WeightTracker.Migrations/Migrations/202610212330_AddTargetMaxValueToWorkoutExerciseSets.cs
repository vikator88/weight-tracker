using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610212330)]
public sealed class AddTargetMaxValueToWorkoutExerciseSets_202610212330 : Migration
{
    public override void Up()
    {
        // Null for every target type except RepsRange, whose min stays in TargetValue
        Alter.Table("WorkoutExerciseSets")
            .AddColumn("TargetMaxValue").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Column("TargetMaxValue").FromTable("WorkoutExerciseSets");
    }
}

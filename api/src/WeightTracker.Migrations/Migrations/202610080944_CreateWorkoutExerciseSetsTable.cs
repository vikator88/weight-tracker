using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080944)]
public sealed class CreateWorkoutExerciseSetsTable_202610080944 : Migration
{
    public override void Up()
    {
        Create.Table("WorkoutExerciseSets")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_WorkoutExerciseSets")
            .WithColumn("WorkoutExerciseId").AsGuid().NotNullable()
            .WithColumn("Position").AsInt32().NotNullable()
            .WithColumn("Count").AsInt32().NotNullable()
            // SetTarget is a closed hierarchy: Reps | Duration | MaxReps
            .WithColumn("TargetType").AsString(20).NotNullable()
            // Null only for MaxReps, which carries no value
            .WithColumn("TargetValue").AsInt32().Nullable();

        Create.ForeignKey("FK_WorkoutExerciseSets_WorkoutExercise")
            .FromTable("WorkoutExerciseSets").ForeignColumn("WorkoutExerciseId")
            .ToTable("WorkoutExercises").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("WorkoutExerciseSets");
    }
}

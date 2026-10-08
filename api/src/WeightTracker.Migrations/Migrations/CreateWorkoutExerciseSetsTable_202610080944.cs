using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080944)]
public sealed class CreateWorkoutExerciseSetsTable_202610080944 : Migration
{
    public override void Up()
    {
        Create.Table("workout_exercise_sets")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_workout_exercise_sets")
            .WithColumn("workout_exercise_id").AsGuid().NotNullable()
            .WithColumn("position").AsInt32().NotNullable()
            .WithColumn("count").AsInt32().NotNullable()
            // SetTarget is a closed hierarchy: Reps | Duration | MaxReps
            .WithColumn("target_type").AsString(20).NotNullable()
            // Null only for MaxReps, which carries no value
            .WithColumn("target_value").AsInt32().Nullable();

        Create.ForeignKey("fk_workout_exercise_sets_workout_exercise")
            .FromTable("workout_exercise_sets").ForeignColumn("workout_exercise_id")
            .ToTable("workout_exercises").PrimaryColumn("id")
            .OnDelete(Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("workout_exercise_sets");
    }
}

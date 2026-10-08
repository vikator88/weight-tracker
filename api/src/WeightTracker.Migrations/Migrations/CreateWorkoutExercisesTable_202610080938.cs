using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080938)]
public sealed class CreateWorkoutExercisesTable_202610080938 : Migration
{
    public override void Up()
    {
        Create.Table("workout_exercises")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_workout_exercises")
            .WithColumn("workout_id").AsGuid().NotNullable()
            .WithColumn("exercise_id").AsGuid().NotNullable()
            // The aggregate holds an ordered collection; the database would otherwise lose it
            .WithColumn("position").AsInt32().NotNullable();

        Create.ForeignKey("fk_workout_exercises_workout")
            .FromTable("workout_exercises").ForeignColumn("workout_id")
            .ToTable("workouts").PrimaryColumn("id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("fk_workout_exercises_exercise")
            .FromTable("workout_exercises").ForeignColumn("exercise_id")
            .ToTable("exercises").PrimaryColumn("id");

        // An exercise appears at most once per workout; its sets are grouped under it
        Create.UniqueConstraint("uq_workout_exercises_workout_exercise")
            .OnTable("workout_exercises").Columns("workout_id", "exercise_id");
    }

    public override void Down()
    {
        Delete.Table("workout_exercises");
    }
}

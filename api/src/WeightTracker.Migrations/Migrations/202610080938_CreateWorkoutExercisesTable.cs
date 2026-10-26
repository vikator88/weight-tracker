using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080938)]
public sealed class CreateWorkoutExercisesTable_202610080938 : Migration
{
    public override void Up()
    {
        Create.Table("WorkoutExercises")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_WorkoutExercises")
            .WithColumn("WorkoutId").AsGuid().NotNullable()
            .WithColumn("ExerciseId").AsGuid().NotNullable()
            // The aggregate holds an ordered collection; the database would otherwise lose it
            .WithColumn("Position").AsInt32().NotNullable();

        Create.ForeignKey("FK_WorkoutExercises_Workout")
            .FromTable("WorkoutExercises").ForeignColumn("WorkoutId")
            .ToTable("Workouts").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        Create.ForeignKey("FK_WorkoutExercises_Exercise")
            .FromTable("WorkoutExercises").ForeignColumn("ExerciseId")
            .ToTable("Exercises").PrimaryColumn("Id");

        // An exercise appears at most once per workout; its sets are grouped under it
        Create.UniqueConstraint("UQ_WorkoutExercises_WorkoutId_ExerciseId")
            .OnTable("WorkoutExercises").Columns("WorkoutId", "ExerciseId");
    }

    public override void Down()
    {
        Delete.Table("WorkoutExercises");
    }
}

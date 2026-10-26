using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610161647)]
public sealed class AddSuperSeriesIdToWorkoutExercises_202610161647 : Migration
{
    public override void Up()
    {
        // Null means the exercise is not part of a superseries, which is right for every existing row.
        // No index or constraint: the id is only unique together with WorkoutId and the aggregate enforces it
        Alter.Table("WorkoutExercises")
            .AddColumn("SuperSeriesId").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Column("SuperSeriesId").FromTable("WorkoutExercises");
    }
}

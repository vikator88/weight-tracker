using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080923)]
public sealed class CreateExercisesTable_202610080923 : Migration
{
    public override void Up()
    {
        Create.Table("Exercises")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_Exercises")
            // Length aligned with ExerciseName.MaxLength
            .WithColumn("Name").AsString(150).NotNullable()
            // Length aligned with Url.MaxLength
            .WithColumn("VideoUrl").AsString(500).Nullable()
            .WithColumn("BodyPart").AsInt32().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Exercises");
    }
}

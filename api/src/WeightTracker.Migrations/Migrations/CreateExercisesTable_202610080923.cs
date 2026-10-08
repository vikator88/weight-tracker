using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080923)]
public sealed class CreateExercisesTable_202610080923 : Migration
{
    public override void Up()
    {
        Create.Table("exercises")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_exercises")
            // Length aligned with Exercise.NameMaxLength
            .WithColumn("name").AsString(150).NotNullable()
            // Length aligned with Exercise.VideoUrlMaxLength
            .WithColumn("video_url").AsString(500).Nullable()
            .WithColumn("body_part").AsInt32().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("exercises");
    }
}

using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080931)]
public sealed class CreateWorkoutsTable_202610080931 : Migration
{
    public override void Up()
    {
        Create.Table("Workouts")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_Workouts")
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("TrainerId").AsGuid().Nullable()
            .WithColumn("WorkoutDate").AsDate().NotNullable();

        Create.ForeignKey("FK_Workouts_User")
            .FromTable("Workouts").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.ForeignKey("FK_Workouts_Trainer")
            .FromTable("Workouts").ForeignColumn("TrainerId")
            .ToTable("Users").PrimaryColumn("Id");

        Create.Index("IX_Workouts_UserId").OnTable("Workouts").OnColumn("UserId").Ascending();
        Create.Index("IX_Workouts_TrainerId").OnTable("Workouts").OnColumn("TrainerId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("Workouts");
    }
}

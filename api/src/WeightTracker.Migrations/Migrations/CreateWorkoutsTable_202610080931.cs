using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

[Migration(202610080931)]
public sealed class CreateWorkoutsTable_202610080931 : Migration
{
    public override void Up()
    {
        Create.Table("workouts")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_workouts")
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("trainer_id").AsGuid().Nullable()
            .WithColumn("workout_date").AsDate().NotNullable();

        Create.ForeignKey("fk_workouts_user")
            .FromTable("workouts").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id");

        Create.ForeignKey("fk_workouts_trainer")
            .FromTable("workouts").ForeignColumn("trainer_id")
            .ToTable("users").PrimaryColumn("id");

        Create.Index("ix_workouts_user_id").OnTable("workouts").OnColumn("user_id").Ascending();
        Create.Index("ix_workouts_trainer_id").OnTable("workouts").OnColumn("trainer_id").Ascending();
    }

    public override void Down()
    {
        Delete.Table("workouts");
    }
}

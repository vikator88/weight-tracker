using FluentMigrator;
using System.Data;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Moves exercise names to one row per language, and replaces the stored body part with a muscle.
/// </summary>
/// <remarks>
/// Written while the Exercises table held no data, so the dropped columns lose nothing.
/// The body part is derived from the muscle in the domain and is no longer stored.
/// </remarks>
[Migration(202610270035)]
public sealed class AddExerciseTranslationsAndMuscle_202610270035 : Migration
{
    public override void Up()
    {
        Delete.Column("Name").Column("BodyPart").FromTable("Exercises");

        Alter.Table("Exercises")
            .AddColumn("Muscle").AsInt32().NotNullable()
            // Length aligned with FreeExerciseDbId.MaxLength
            .AddColumn("FreeExerciseDbId").AsString(128).Nullable();

        // Postgres treats nulls as distinct, so exercises without a dataset id never conflict
        Create.UniqueConstraint("UQ_Exercises_FreeExerciseDbId")
            .OnTable("Exercises").Column("FreeExerciseDbId");

        Create.Table("ExerciseTranslations")
            .WithColumn("Id").AsGuid().NotNullable().PrimaryKey("PK_ExerciseTranslations")
            .WithColumn("ExerciseId").AsGuid().NotNullable()
            // Length aligned with LanguageCode.MaxLength
            .WithColumn("LanguageCode").AsString(2).NotNullable()
            // Length aligned with ExerciseName.MaxLength
            .WithColumn("Name").AsString(150).NotNullable();

        Create.ForeignKey("FK_ExerciseTranslations_Exercise")
            .FromTable("ExerciseTranslations").ForeignColumn("ExerciseId")
            .ToTable("Exercises").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);

        // An exercise has at most one name per language
        Create.UniqueConstraint("UQ_ExerciseTranslations_ExerciseId_LanguageCode")
            .OnTable("ExerciseTranslations").Columns("ExerciseId", "LanguageCode");
    }

    public override void Down()
    {
        Delete.Table("ExerciseTranslations");

        Delete.UniqueConstraint("UQ_Exercises_FreeExerciseDbId").FromTable("Exercises");

        Delete.Column("FreeExerciseDbId").Column("Muscle").FromTable("Exercises");

        // Defaults only so the rollback also runs on a table with rows; the original columns had none
        Alter.Table("Exercises")
            .AddColumn("Name").AsString(150).NotNullable().WithDefaultValue(string.Empty)
            .AddColumn("BodyPart").AsInt32().NotNullable().WithDefaultValue(0);
    }
}

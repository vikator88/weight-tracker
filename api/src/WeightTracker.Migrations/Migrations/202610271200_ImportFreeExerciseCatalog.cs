using System.Text.Json;
using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Inserts every exercise of the free exercise dataset (<c>infra/exercises.json</c>) with its
/// dataset id, its primary muscle, its names in English and Spanish and, when one was found, a
/// YouTube technique video.
/// </summary>
/// <remarks>
/// The rows come from <c>Data/ExerciseCatalog.json</c>, embedded in this assembly. Every id in it is
/// fixed, so <see cref="Down"/> removes exactly what <see cref="Up"/> inserted and the seed can reference
/// catalog rows. Down fails while a workout references a catalog exercise: that foreign key is restricted
/// on purpose, and the workout has to go first.
/// </remarks>
[Migration(202610271200)]
public sealed class ImportFreeExerciseCatalog_202610271200 : Migration
{
    public const string CatalogResource = "WeightTracker.Migrations.Data.ExerciseCatalog.json";

    /// <summary>
    /// Dataset muscle names to their <c>Muscles</c> values. The migrations project references no other
    /// project, so the enum is not reachable and must stay aligned by hand.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> MuscleValues = new Dictionary<string, int>
    {
        ["abdominals"] = 1,
        ["abductors"] = 2,
        ["adductors"] = 3,
        ["biceps"] = 4,
        ["calves"] = 5,
        ["chest"] = 6,
        ["forearms"] = 7,
        ["glutes"] = 8,
        ["hamstrings"] = 9,
        ["lats"] = 10,
        ["lower back"] = 11,
        ["middle back"] = 12,
        ["neck"] = 13,
        ["quadriceps"] = 14,
        ["shoulders"] = 15,
        ["traps"] = 16,
        ["triceps"] = 17,
    };

    public override void Up()
    {
        foreach (var exercise in LoadCatalog())
        {
            if (MuscleValues.TryGetValue(exercise.Muscle, out var muscle) == false)
                throw new InvalidOperationException(
                    $"'{exercise.Muscle}' of catalog exercise '{exercise.FreeExerciseDbId}' is not a known muscle");

            Insert.IntoTable("Exercises").Row(new
            {
                exercise.Id,
                exercise.FreeExerciseDbId,
                // Null when no clear technique video was found. Length aligned with Url.MaxLength
                exercise.VideoUrl,
                Muscle = muscle,
            });

            foreach (var translation in exercise.Translations)
            {
                Insert.IntoTable("ExerciseTranslations").Row(new
                {
                    translation.Id,
                    ExerciseId = exercise.Id,
                    translation.LanguageCode,
                    translation.Name,
                });
            }
        }
    }

    public override void Down()
    {
        // Translations go with their exercise through FK_ExerciseTranslations_Exercise
        foreach (var exercise in LoadCatalog())
            Delete.FromTable("Exercises").Row(new { exercise.Id });
    }

    public static IReadOnlyList<CatalogExercise> LoadCatalog()
    {
        using var stream = typeof(ImportFreeExerciseCatalog_202610271200).Assembly.GetManifestResourceStream(CatalogResource)
            ?? throw new InvalidOperationException($"Embedded resource '{CatalogResource}' was not found");

        return JsonSerializer.Deserialize<List<CatalogExercise>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Embedded resource '{CatalogResource}' is empty");
    }

    public sealed record CatalogExercise(
        Guid Id,
        string FreeExerciseDbId,
        string Muscle,
        string? VideoUrl,
        IReadOnlyList<CatalogTranslation> Translations);

    public sealed record CatalogTranslation(Guid Id, string LanguageCode, string Name);
}

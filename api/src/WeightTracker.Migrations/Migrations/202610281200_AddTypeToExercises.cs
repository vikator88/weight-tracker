using System.Text.Json;
using FluentMigrator;

namespace WeightTracker.Migrations.Migrations;

/// <summary>
/// Adds the mandatory exercise type and fills it for every catalog exercise from the <c>type</c> of
/// <c>Data/ExerciseCatalog.json</c>.
/// </summary>
/// <remarks>
/// The column is added nullable, filled by catalog id, then made non-nullable, so a row the catalog does
/// not know about fails the migration instead of getting a type nobody chose. The catalog import migration
/// already ran in existing databases, which is why the type is not inserted there.
/// </remarks>
[Migration(202610281200)]
public sealed class AddTypeToExercises_202610281200 : Migration
{
    /// <summary>
    /// Catalog type names to their <c>ExerciseTypes</c> values. The migrations project references no other
    /// project, so the enum is not reachable and must stay aligned by hand.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> TypeValues = new Dictionary<string, int>
    {
        ["strength"] = 1,
        ["cardio"] = 2,
        ["stretching"] = 3,
    };

    public override void Up()
    {
        // Resolved before any expression is queued, so an invalid catalog changes nothing
        var types = LoadCatalogTypes()
            .Select(exercise => TypeValues.TryGetValue(exercise.Type ?? string.Empty, out var type)
                ? (exercise.Id, Type: type)
                : throw new InvalidOperationException(
                    $"'{exercise.Type}' of catalog exercise '{exercise.FreeExerciseDbId}' is not a known exercise type"))
            .ToList();

        Alter.Table("Exercises").AddColumn("Type").AsInt32().Nullable();

        foreach (var (id, type) in types)
            Update.Table("Exercises").Set(new { Type = type }).Where(new { Id = id });

        Alter.Column("Type").OnTable("Exercises").AsInt32().NotNullable();
    }

    public override void Down()
    {
        Delete.Column("Type").FromTable("Exercises");
    }

    private static IReadOnlyList<CatalogExerciseType> LoadCatalogTypes()
    {
        var resource = ImportFreeExerciseCatalog_202610271200.CatalogResource;

        using var stream = typeof(AddTypeToExercises_202610281200).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded resource '{resource}' was not found");

        return JsonSerializer.Deserialize<List<CatalogExerciseType>>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException($"Embedded resource '{resource}' is empty");
    }

    private sealed record CatalogExerciseType(Guid Id, string FreeExerciseDbId, string? Type);
}

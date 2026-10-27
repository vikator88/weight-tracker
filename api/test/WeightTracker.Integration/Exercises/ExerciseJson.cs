using System.Text.Json;

namespace WeightTracker.Integration.Exercises;

/// <summary>
/// Reads the exercise fields that several endpoint tests assert on.
/// </summary>
internal static class ExerciseJson
{
    /// <summary>The <c>translations</c> of an exercise as (languageCode, name) pairs.</summary>
    public static IEnumerable<(string?, string?)> Translations(JsonElement exercise)
        => exercise.GetProperty("translations").EnumerateArray()
            .Select(translation => (translation.GetProperty("languageCode").GetString(), translation.GetProperty("name").GetString()));
}

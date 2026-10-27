using WeightTracker.Domain.Exercises;

namespace WeightTracker.Api.Endpoints.Exercises.Dtos;

public sealed record ExerciseResponse(
    Guid Id,
    string? FreeExerciseDbId,
    string? VideoUrl,
    string Muscle,
    string BodyPart,
    IReadOnlyList<ExerciseTranslationResponse> Translations)
{
    public static ExerciseResponse From(Exercise exercise)
        => new(
            exercise.Id.Value,
            exercise.FreeExerciseDbId?.Value,
            exercise.VideoUrl?.Value,
            exercise.Muscle.ToString(),
            exercise.BodyPart.ToString(),
            exercise.Translations.Select(ExerciseTranslationResponse.From).ToList());
}

public sealed record ExerciseTranslationResponse(string LanguageCode, string Name)
{
    public static ExerciseTranslationResponse From(ExerciseTranslation translation)
        => new(translation.LanguageCode.Value, translation.Name.Value);
}

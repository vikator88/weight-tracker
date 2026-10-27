using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// The name of an exercise, such as "Peso Muerto" or "Press Banca".
/// </summary>
/// <remarks>
/// Kept apart from <c>PersonName</c> on purpose: the bounds differ (3 to 150 characters here,
/// 1 to 100 there) and it names a movement, not a person. Only length is constrained.
/// </remarks>
public sealed record ExerciseName
{
    /// <summary>Minimum length accepted, after trimming.</summary>
    public const int MinLength = 3;

    /// <summary>
    /// Maximum length accepted. The ExerciseTranslations.Name column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 150;

    public string Value { get; }

    private ExerciseName(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidExerciseNameException">When the value is empty, too short or too long.</exception>
    public static ExerciseName From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidExerciseNameException(value ?? string.Empty);

        var normalized = value.Trim();

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
            throw new InvalidExerciseNameException(value);

        return new ExerciseName(normalized);
    }

    public override string ToString() => Value;
}

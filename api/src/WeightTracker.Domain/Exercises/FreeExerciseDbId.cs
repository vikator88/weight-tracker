using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// The id of an entry in the free exercise dataset (<c>infra/exercises.json</c>), such as "Barbell_Squat".
/// </summary>
/// <remarks>
/// A string from an external dataset, so it does not use <c>Id</c>. Only length is constrained;
/// whether the entry exists in the dataset is not checked.
/// </remarks>
public sealed record FreeExerciseDbId
{
    /// <summary>
    /// Maximum length accepted. The Exercises.FreeExerciseDbId column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 128;

    public string Value { get; }

    private FreeExerciseDbId(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidFreeExerciseDbIdException">When the value is empty or too long.</exception>
    public static FreeExerciseDbId From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidFreeExerciseDbIdException(value ?? string.Empty);

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new InvalidFreeExerciseDbIdException(value);

        return new FreeExerciseDbId(normalized);
    }

    public override string ToString() => Value;
}

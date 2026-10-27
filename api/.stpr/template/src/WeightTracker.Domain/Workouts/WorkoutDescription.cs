using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Optional free text explaining the workout.
/// </summary>
public sealed record WorkoutDescription
{
    /// <summary>
    /// The Workouts.Description column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 1024;

    public string Value { get; }

    private WorkoutDescription(string value)
    {
        Value = value;
    }

    /// <returns>The trimmed description, or <c>null</c> when the value is null or blank.</returns>
    /// <exception cref="InvalidWorkoutDescriptionException">When the value is too long once trimmed.</exception>
    public static WorkoutDescription? From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidWorkoutDescriptionException($"The workout description cannot be longer than {MaxLength} characters");

        return new WorkoutDescription(trimmed);
    }

    public override string ToString() => Value;
}

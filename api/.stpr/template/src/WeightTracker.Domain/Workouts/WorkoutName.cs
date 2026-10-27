using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Name the workout is listed and recognised by. It is not unique: two workouts may share a name.
/// </summary>
public sealed record WorkoutName
{
    /// <summary>
    /// The Workouts.Name column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 256;

    public string Value { get; }

    private WorkoutName(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidWorkoutNameException">When the value is blank or too long once trimmed.</exception>
    public static WorkoutName From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidWorkoutNameException("The workout name is required");

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            throw new InvalidWorkoutNameException($"The workout name cannot be longer than {MaxLength} characters");

        return new WorkoutName(trimmed);
    }

    public override string ToString() => Value;
}

namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>workout_exercise_sets</c> table.</summary>
public class WorkoutExerciseSetEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutExerciseId { get; set; }

    /// <summary>Preserves the order of the prescriptions of one exercise.</summary>
    public int Position { get; set; }

    public int Count { get; set; }

    /// <summary>Discriminator of the SetTarget hierarchy: Reps, Duration or MaxReps.</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Null only for MaxReps, which carries no value.</summary>
    public int? TargetValue { get; set; }
}

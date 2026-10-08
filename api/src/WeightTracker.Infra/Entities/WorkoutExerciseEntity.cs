namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>workout_exercises</c> table.</summary>
/// <remarks>
/// <c>ExerciseId</c> stays a plain foreign key column. The Exercise aggregate is loaded
/// separately by the repository rather than navigated into from the Workout aggregate.
/// </remarks>
public class WorkoutExerciseEntity
{
    public Guid Id { get; set; }

    public Guid WorkoutId { get; set; }

    public Guid ExerciseId { get; set; }

    /// <summary>Preserves the order the aggregate holds its exercises in.</summary>
    public int Position { get; set; }

    /// <summary>Navigation inside the Workout aggregate boundary.</summary>
    public List<WorkoutExerciseSetEntity> Sets { get; set; } = [];
}

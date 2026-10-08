namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>workouts</c> table.</summary>
/// <remarks>
/// <c>UserId</c> and <c>TrainerId</c> stay plain foreign key columns: the User aggregate
/// is referenced by identifier only, never navigated into from here.
/// </remarks>
public class WorkoutEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? TrainerId { get; set; }

    public DateOnly WorkoutDate { get; set; }

    /// <summary>Navigation inside the Workout aggregate boundary.</summary>
    public List<WorkoutExerciseEntity> WorkoutExercises { get; set; } = [];
}

using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class WorkoutExerciseOrderMismatchException : DomainException
{
    public WorkoutExerciseOrderMismatchException(Id workoutId)
        : base($"The new order must list every exercise of workout '{workoutId}' exactly once")
    { }
}

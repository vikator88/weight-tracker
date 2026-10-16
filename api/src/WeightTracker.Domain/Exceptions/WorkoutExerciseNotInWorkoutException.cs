using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class WorkoutExerciseNotInWorkoutException : DomainException
{
    public WorkoutExerciseNotInWorkoutException(Id workoutExerciseId, Id workoutId)
        : base($"Workout exercise '{workoutExerciseId}' is not part of workout '{workoutId}'")
    { }
}

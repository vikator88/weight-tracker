using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class ExerciseNotInWorkoutException : DomainException
{
    public ExerciseNotInWorkoutException(string exerciseName, Id workoutId)
        : base($"Exercise '{exerciseName}' is not part of workout '{workoutId}'")
    { }
}

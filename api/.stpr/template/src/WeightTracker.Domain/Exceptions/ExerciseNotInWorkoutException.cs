using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class ExerciseNotInWorkoutException : DomainException
{
    public ExerciseNotInWorkoutException(Id exerciseId, Id workoutId)
        : base($"Exercise '{exerciseId}' is not part of workout '{workoutId}'")
    { }
}

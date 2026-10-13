using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class ExerciseAlreadyInWorkoutException : DomainException
{
    public ExerciseAlreadyInWorkoutException(Id exerciseId, Id workoutId)
        : base($"Exercise '{exerciseId}' is already part of workout '{workoutId}'")
    { }
}

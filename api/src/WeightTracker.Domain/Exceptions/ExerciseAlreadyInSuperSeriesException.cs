using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class ExerciseAlreadyInSuperSeriesException : DomainException
{
    public ExerciseAlreadyInSuperSeriesException(Id exerciseId, Id workoutId)
        : base($"Exercise '{exerciseId}' of workout '{workoutId}' already belongs to a superseries")
    { }
}

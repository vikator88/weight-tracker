using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class DuplicateExerciseInSuperSeriesException : DomainException
{
    public DuplicateExerciseInSuperSeriesException(Id workoutExerciseId)
        : base($"Workout exercise '{workoutExerciseId}' was provided more than once for the same superseries")
    { }
}

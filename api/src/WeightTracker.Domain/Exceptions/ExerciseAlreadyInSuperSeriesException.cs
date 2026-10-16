using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class ExerciseAlreadyInSuperSeriesException : DomainException
{
    public ExerciseAlreadyInSuperSeriesException(Id workoutExerciseId, Id workoutId)
        : base($"Workout exercise '{workoutExerciseId}' of workout '{workoutId}' already belongs to a superseries")
    { }
}

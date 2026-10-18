using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class ExerciseIdMismatchException : ApplicationLayerException
{
    public ExerciseIdMismatchException(Id routeExerciseId, Id bodyExerciseId)
        : base($"The exercise '{bodyExerciseId}' in the body does not match the exercise '{routeExerciseId}' in the route")
    { }
}

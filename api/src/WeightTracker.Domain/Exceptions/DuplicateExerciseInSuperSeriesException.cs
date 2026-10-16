using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class DuplicateExerciseInSuperSeriesException : DomainException
{
    public DuplicateExerciseInSuperSeriesException(Id exerciseId)
        : base($"Exercise '{exerciseId}' was provided more than once for the same superseries")
    { }
}

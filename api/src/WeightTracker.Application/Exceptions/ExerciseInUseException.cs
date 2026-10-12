using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class ExerciseInUseException : ApplicationLayerException
{
    public ExerciseInUseException(Id exerciseId)
        : base($"Exercise '{exerciseId}' is included in at least one workout and cannot be deleted")
    { }
}

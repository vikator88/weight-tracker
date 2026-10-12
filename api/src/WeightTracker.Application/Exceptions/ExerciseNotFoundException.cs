using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class ExerciseNotFoundException : ApplicationLayerException
{
    public ExerciseNotFoundException(Id exerciseId) : base($"Exercise '{exerciseId}' was not found")
    { }
}

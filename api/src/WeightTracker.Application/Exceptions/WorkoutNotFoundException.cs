using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class WorkoutNotFoundException : ApplicationLayerException
{
    public WorkoutNotFoundException(Id workoutId) : base($"Workout '{workoutId}' was not found")
    { }
}

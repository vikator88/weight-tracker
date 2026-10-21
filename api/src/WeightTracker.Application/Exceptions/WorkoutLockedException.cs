using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a caller who may edit a workout tries to do so after it has started.
/// </summary>
public sealed class WorkoutLockedException : ApplicationLayerException
{
    public WorkoutLockedException(Id workoutId, WorkoutStatus status)
        : base($"Workout '{workoutId}' is {status} and can no longer be edited")
    { }
}

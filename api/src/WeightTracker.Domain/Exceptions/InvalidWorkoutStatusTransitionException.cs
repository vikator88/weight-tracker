using WeightTracker.Domain.Workouts;

namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidWorkoutStatusTransitionException : DomainException
{
    public InvalidWorkoutStatusTransitionException(WorkoutStatus from, WorkoutStatus to)
        : base($"A workout cannot go from '{from}' to '{to}'")
    { }
}

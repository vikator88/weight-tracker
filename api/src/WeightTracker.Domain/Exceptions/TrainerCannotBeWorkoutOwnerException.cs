using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class TrainerCannotBeWorkoutOwnerException : DomainException
{
    public TrainerCannotBeWorkoutOwnerException(Id userId)
        : base($"User '{userId}' cannot be the trainer of their own workout")
    { }
}

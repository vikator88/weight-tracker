using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class UserAlreadyDeactivatedException : DomainException
{
    public UserAlreadyDeactivatedException(Id userId)
        : base($"User '{userId}' is already deactivated")
    { }
}

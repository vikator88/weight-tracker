using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class UserAlreadyActiveException : DomainException
{
    public UserAlreadyActiveException(Id userId)
        : base($"User '{userId}' is already active")
    { }
}

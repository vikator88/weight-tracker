using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a user would be restored as active with a deactivation date, or as deactivated without one.
/// </summary>
public sealed class InvalidUserActivationStateException : DomainException
{
    public InvalidUserActivationStateException(Id userId)
        : base($"User '{userId}' must have a deactivation date exactly when it is deactivated")
    { }
}

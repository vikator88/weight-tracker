namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a plain-text password does not meet the strength requirements.
/// </summary>
/// <remarks>
/// The rejected password never reaches the message, so the unmet rule is reported
/// without echoing the credential back to the caller or into a log.
/// </remarks>
public sealed class InvalidPasswordException : DomainException
{
    public InvalidPasswordException(string reason)
        : base($"The password does not meet the strength requirements: {reason}")
    { }
}

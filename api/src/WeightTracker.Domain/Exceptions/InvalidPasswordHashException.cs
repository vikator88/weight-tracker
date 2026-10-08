namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Raised when a stored password hash cannot be restored.
/// </summary>
/// <remarks>
/// The offending value is credential material, so it never reaches the message.
/// </remarks>
public sealed class InvalidPasswordHashException : DomainException
{
    public InvalidPasswordHashException()
        : base("The stored password hash is not valid")
    { }
}

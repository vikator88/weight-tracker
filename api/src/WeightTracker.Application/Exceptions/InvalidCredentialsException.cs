namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a login attempt fails. The message is deliberately generic: an unknown
/// email and a wrong password must be indistinguishable to the caller.
/// </summary>
public sealed class InvalidCredentialsException : ApplicationLayerException
{
    public InvalidCredentialsException() : base("Invalid email or password")
    { }
}

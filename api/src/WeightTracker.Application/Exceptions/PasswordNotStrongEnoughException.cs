namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a login attempt fails. The message is deliberately generic: an unknown
/// email and a wrong password must be indistinguishable to the caller.
/// </summary>
public sealed class PasswordNotStrongEnoughException : ApplicationLayerException
{
    public PasswordNotStrongEnoughException() : base("Password is not strong enough. Min 8 characters, 1 capital letter, 1 number and 1 special character")
    { }
}

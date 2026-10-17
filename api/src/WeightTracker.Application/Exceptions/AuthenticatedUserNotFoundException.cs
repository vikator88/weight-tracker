namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a valid access token belongs to a user that no longer exists. The message is
/// deliberately generic so the response does not reveal that the user was removed.
/// </summary>
public sealed class AuthenticatedUserNotFoundException : ApplicationLayerException
{
    public AuthenticatedUserNotFoundException() : base("Authentication is required")
    { }
}

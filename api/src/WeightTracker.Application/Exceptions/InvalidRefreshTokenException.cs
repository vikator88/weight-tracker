namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a refresh token is unknown, expired or already revoked.
/// The three cases are intentionally not distinguished to the caller.
/// </summary>
public sealed class InvalidRefreshTokenException : ApplicationLayerException
{
    public InvalidRefreshTokenException() : base("The refresh token is not valid")
    { }
}

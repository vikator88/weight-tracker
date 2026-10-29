namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a deactivated user presents valid credentials or a valid refresh token. It carries a stable
/// <see cref="Code"/> so clients can tell it apart from wrong credentials and point the user to support.
/// </summary>
public sealed class UserDeactivatedException : ApplicationLayerException
{
    public const string Code = "USER_DEACTIVATED";

    public UserDeactivatedException() : base("This account is deactivated")
    { }
}

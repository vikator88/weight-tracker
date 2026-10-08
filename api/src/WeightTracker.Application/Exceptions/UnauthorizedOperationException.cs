namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when an authenticated caller is not allowed to perform the requested operation.
/// </summary>
public sealed class UnauthorizedOperationException : ApplicationLayerException
{
    public UnauthorizedOperationException(string operation)
        : base($"The caller is not allowed to perform '{operation}'")
    { }
}

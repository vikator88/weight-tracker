using WeightTracker.Application.Exceptions;

namespace WeightTracker.Infra.Exceptions;

/// <summary>
/// Raised when persisted data cannot be turned back into a valid domain object.
/// </summary>
/// <remarks>
/// This signals corrupted or inconsistent storage, not bad caller input, so the
/// ExceptionMapper resolves it to a 500.
/// </remarks>
public sealed class PersistenceMappingException : ApplicationLayerException
{
    public PersistenceMappingException(string message) : base(message)
    { }

    public PersistenceMappingException(string message, Exception innerException)
        : base(message, innerException)
    { }
}

namespace WeightTracker.Domain.Exceptions;

/// <summary>
/// Base type for every custom exception thrown by the Domain layer.
/// The API layer maps these to HTTP responses through the ExceptionMapper.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    { }

    protected DomainException(string message, Exception innerException) : base(message, innerException)
    { }
}

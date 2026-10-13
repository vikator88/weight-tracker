namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidEffortException : DomainException
{
    public InvalidEffortException(int value)
        : base($"'{value}' is not a known effort")
    { }
}

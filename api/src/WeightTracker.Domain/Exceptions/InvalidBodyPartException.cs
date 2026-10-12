namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidBodyPartException : DomainException
{
    public InvalidBodyPartException(int value)
        : base($"'{value}' is not a known body part")
    { }
}

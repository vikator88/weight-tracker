namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidPersonNameException : DomainException
{
    public InvalidPersonNameException(string value)
        : base($"'{value}' is not a valid person name")
    { }
}

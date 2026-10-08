namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidIdException : DomainException
{
    public InvalidIdException(string value)
        : base($"'{value}' is not a valid identifier")
    { }
}

namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidUrlException : DomainException
{
    public InvalidUrlException(string value)
        : base($"'{value}' is not a valid url")
    { }
}

namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidLanguageCodeException : DomainException
{
    public InvalidLanguageCodeException(string value)
        : base($"'{value}' is not a valid language code")
    { }
}

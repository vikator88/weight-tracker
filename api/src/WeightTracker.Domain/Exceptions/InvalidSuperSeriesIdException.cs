namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidSuperSeriesIdException : DomainException
{
    public InvalidSuperSeriesIdException(int value)
        : base($"'{value}' is not a valid superseries id, it must be a positive integer")
    { }
}

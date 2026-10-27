namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidWorkoutDescriptionException : DomainException
{
    public InvalidWorkoutDescriptionException(string message)
        : base(message)
    { }
}

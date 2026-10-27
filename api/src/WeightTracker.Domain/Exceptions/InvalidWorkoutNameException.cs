namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidWorkoutNameException : DomainException
{
    public InvalidWorkoutNameException(string message)
        : base(message)
    { }
}

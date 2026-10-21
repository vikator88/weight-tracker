namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidWorkoutStatusException : DomainException
{
    public InvalidWorkoutStatusException(int value)
        : base($"'{value}' is not a known workout status")
    { }
}

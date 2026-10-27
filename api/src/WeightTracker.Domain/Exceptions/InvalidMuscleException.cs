namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidMuscleException : DomainException
{
    public InvalidMuscleException(int value)
        : base($"'{value}' is not a known muscle")
    { }
}

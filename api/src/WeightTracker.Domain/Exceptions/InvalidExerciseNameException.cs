namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidExerciseNameException : DomainException
{
    public InvalidExerciseNameException(string value)
        : base($"'{value}' is not a valid exercise name")
    { }
}

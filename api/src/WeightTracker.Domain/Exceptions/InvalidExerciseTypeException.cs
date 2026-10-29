namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidExerciseTypeException : DomainException
{
    public InvalidExerciseTypeException(int value)
        : base($"'{value}' is not a known exercise type")
    { }
}

namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidFreeExerciseDbIdException : DomainException
{
    public InvalidFreeExerciseDbIdException(string value)
        : base($"'{value}' is not a valid free exercise db id")
    { }
}

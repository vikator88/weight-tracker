namespace WeightTracker.Domain.Exceptions;

public sealed class SuperSeriesRequiresTwoExercisesException : DomainException
{
    public SuperSeriesRequiresTwoExercisesException(int provided)
        : base($"A superseries needs at least two exercises, but {provided} were provided")
    { }
}

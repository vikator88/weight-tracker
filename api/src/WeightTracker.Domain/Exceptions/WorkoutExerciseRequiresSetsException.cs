namespace WeightTracker.Domain.Exceptions;

public sealed class WorkoutExerciseRequiresSetsException : DomainException
{
    public WorkoutExerciseRequiresSetsException()
        : base("A workout exercise needs at least one set prescription")
    { }
}

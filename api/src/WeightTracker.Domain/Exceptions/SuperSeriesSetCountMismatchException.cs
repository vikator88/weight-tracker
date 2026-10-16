using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class SuperSeriesSetCountMismatchException : DomainException
{
    public SuperSeriesSetCountMismatchException(Id workoutId)
        : base($"The exercises of a superseries in workout '{workoutId}' must all have the same number of sets")
    { }
}

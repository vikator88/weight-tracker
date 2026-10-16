using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Domain.Exceptions;

public sealed class SuperSeriesNotFoundException : DomainException
{
    public SuperSeriesNotFoundException(SuperSeriesId superSeriesId, Id workoutId)
        : base($"Superseries '{superSeriesId}' does not exist in workout '{workoutId}'")
    { }
}

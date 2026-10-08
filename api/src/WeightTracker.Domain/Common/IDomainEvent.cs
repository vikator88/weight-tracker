namespace WeightTracker.Domain.Common;

public interface IDomainEvent
{
    DateTime OccuredOnUtc { get; }
}
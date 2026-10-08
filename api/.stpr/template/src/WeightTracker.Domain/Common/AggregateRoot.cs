namespace WeightTracker.Domain.Common;

/// <summary>
/// This calss represents the base AggregateRoot for DomainDrivenDesing
/// All changes in model will be through this class
/// </summary>
public abstract class AggregateRoot : Entity
{

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void ClearDomainEvents() => _domainEvents.Clear();

}
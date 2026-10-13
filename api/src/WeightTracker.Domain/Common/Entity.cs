namespace WeightTracker.Domain.Common;

/// <summary>
/// This class is the Entity base for DomainDrivenDesign
/// </summary>
/// <remarks>
/// Identity equality is based on the concrete type plus the identifier. Reference equality
/// would make an entity rehydrated from persistence fail to match an equal instance
/// already held elsewhere.
/// </remarks>
public abstract class Entity : IEquatable<Entity>
{
    public Id Id { get; protected set; } = Id.New();

    public bool IsNew { get; protected set; }

    public bool Equals(Entity? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}

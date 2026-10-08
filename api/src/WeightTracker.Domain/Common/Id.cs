using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Common;

/// <summary>
/// Shared identifier for every entity and aggregate root of the system.
/// This is the only approved generation path for system-generated identifiers.
/// </summary>
public sealed record Id
{
    public Guid Value { get; }

    private Id(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Generates a new identifier. This is the single generation point of the domain.
    /// </summary>
    public static Id New() => new(Guid.CreateVersion7());

    /// <summary>
    /// Rebuilds an identifier from an existing value (persistence, external input).
    /// </summary>
    /// <exception cref="InvalidIdException">When the value is empty.</exception>
    public static Id From(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidIdException(value.ToString());

        return new Id(value);
    }

    /// <summary>
    /// Rebuilds an identifier from its textual representation.
    /// </summary>
    /// <exception cref="InvalidIdException">When the text is not a valid non-empty identifier.</exception>
    public static Id From(string value)
    {
        if (Guid.TryParse(value, out var parsed) == false)
            throw new InvalidIdException(value);

        return From(parsed);
    }

    public static bool TryFrom(string? value, out Id? id)
    {
        id = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (Guid.TryParse(value, out var parsed) == false || parsed == Guid.Empty)
            return false;

        id = new Id(parsed);
        return true;
    }

    public override string ToString() => Value.ToString();
}

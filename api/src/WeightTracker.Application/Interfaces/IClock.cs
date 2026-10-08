namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Source of the current time. Keeps token expiry logic testable without freezing
/// the system clock.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

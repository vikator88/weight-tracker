using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

public sealed record SetPrescription
{
    public int count { get; }

    public SetTarget target { get; }

    /// <exception cref="InvalidSetPrescriptionException">When the count is lower than 1.</exception>
    public SetPrescription(int count, SetTarget target)
    {
        if (count < 1)
            throw new InvalidSetPrescriptionException($"count must be at least 1, got {count}");

        this.count = count;
        this.target = target;
    }

    public override string ToString()
    {
        return $"{count}x{target}";
    }
}

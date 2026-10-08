namespace WeightTracker.Domain.Workouts;

public sealed record SetPrescription(int count, SetTarget target)
{
    public override string ToString()
    {
        return $"{count}x{target}";
    }
}
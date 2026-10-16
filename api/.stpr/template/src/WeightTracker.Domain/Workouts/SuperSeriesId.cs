using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// Label of a superseries. Only unique inside its workout, so two workouts can both have superseries 1.
/// </summary>
public sealed record SuperSeriesId
{
    public int Value { get; }

    private SuperSeriesId(int value)
    {
        Value = value;
    }

    /// <exception cref="InvalidSuperSeriesIdException">When the value is not a positive integer.</exception>
    public static SuperSeriesId From(int value)
    {
        if (value < 1)
            throw new InvalidSuperSeriesIdException(value);

        return new SuperSeriesId(value);
    }

    public override string ToString() => Value.ToString();
}

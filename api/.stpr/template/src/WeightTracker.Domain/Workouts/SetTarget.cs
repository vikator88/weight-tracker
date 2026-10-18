using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

public abstract record SetTarget
{
    /// <summary>
    /// When we want to do a fixed number of reps (i.e. BenchPress 2x15)
    /// </summary>
    /// <param name="value">Number of reps per set</param>
    public sealed record Reps : SetTarget
    {
        public int value { get; }

        /// <exception cref="InvalidSetPrescriptionException">When the value is lower than 1.</exception>
        public Reps(int value)
        {
            if (value < 1)
                throw new InvalidSetPrescriptionException($"reps must be at least 1, got {value}");

            this.value = value;
        }

        public override string ToString()
        {
            return $"{value.ToString()} reps.";
        }
    }

    /// <summary>
    /// When we want to do a fixed amount of time (i.e. Plank 3x40)
    /// </summary>
    /// <param name="value">Number of seconds per set</param>
    public sealed record Duration : SetTarget
    {
        public int value { get; }

        /// <exception cref="InvalidSetPrescriptionException">When the value is lower than 1.</exception>
        public Duration(int value)
        {
            if (value < 1)
                throw new InvalidSetPrescriptionException($"duration must be at least 1 second, got {value}");

            this.value = value;
        }

        public override string ToString()
        {
            return $"{value.ToString()} s.";
        }
    }

    /// <summary>
    /// When we want to do the exercise until we dead (i.e. LegCurl 2xMax)
    /// </summary>
    public sealed record MaxReps() : SetTarget
    {
        public override string ToString()
        {
            return $"max.";
        }
    }
}
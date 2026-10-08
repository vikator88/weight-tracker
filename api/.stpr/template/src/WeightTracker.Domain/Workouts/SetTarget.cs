using System.Reflection.Metadata.Ecma335;

namespace WeightTracker.Domain.Workouts;

public abstract record SetTarget
{
    /// <summary>
    /// When we want to do a fixed number of reps (i.e. BenchPress 2x15)
    /// </summary>
    /// <param name="value">Number of reps per set</param>
    public sealed record Reps(int value) : SetTarget
    {
        public override string ToString()
        {
            return $"{value.ToString()} reps.";
        }
    }

    /// <summary>
    /// When we want to do a fixed amount of time (i.e. Plank 3x40)
    /// </summary>
    /// <param name="value">Number of seconds per set</param>
    public sealed record Duration(int value) : SetTarget
    {
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
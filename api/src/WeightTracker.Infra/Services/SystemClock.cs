using WeightTracker.Application.Interfaces;

namespace WeightTracker.Infra.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

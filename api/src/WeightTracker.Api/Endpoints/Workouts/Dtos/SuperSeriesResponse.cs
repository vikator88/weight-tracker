using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

public sealed record SuperSeriesResponse(int Id)
{
    public static SuperSeriesResponse From(SuperSeriesId superSeriesId) => new(superSeriesId.Value);
}

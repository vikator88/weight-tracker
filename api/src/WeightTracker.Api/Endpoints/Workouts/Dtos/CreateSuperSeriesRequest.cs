using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>POST /workouts/{workoutId}/superseries</c>. Only structural validation lives here;
/// how many entries a superseries needs is enforced by the domain.
/// </summary>
public sealed record CreateSuperSeriesRequest
{
    /// <summary>
    /// Identifiers of the workout exercise entries to group (the <c>id</c> of each entry in
    /// <c>GET /workouts</c>), not the catalog <c>exerciseId</c>.
    /// </summary>
    [Required]
    public IReadOnlyList<Guid>? ExerciseIds { get; init; }
}

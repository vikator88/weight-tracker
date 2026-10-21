using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>PUT /workouts/{workoutId}/status</c>. Only structural validation lives here;
/// which moves are allowed belongs to the domain and the application layer.
/// </summary>
public sealed record ChangeWorkoutStatusRequest
{
    /// <summary>
    /// Status to move the workout to: <c>DRAFT</c>, <c>PUBLISHED</c>, <c>STARTED</c> or <c>FINISHED</c>,
    /// or their number (1 to 4).
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter<WorkoutStatus>))]
    public WorkoutStatus? Status { get; init; }
}

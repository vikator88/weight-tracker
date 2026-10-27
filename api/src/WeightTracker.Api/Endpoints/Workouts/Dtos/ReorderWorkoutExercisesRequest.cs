using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

/// <summary>
/// Body of <c>PUT /workouts/{workoutId}/exercises/order</c>. Only structural validation lives here;
/// that the list matches the exercises of the workout is enforced by the domain.
/// </summary>
public sealed record ReorderWorkoutExercisesRequest
{
    /// <summary>
    /// Catalog <c>exerciseId</c> of every exercise of the workout, each once, in the new order.
    /// </summary>
    [Required]
    public IReadOnlyList<Guid>? ExerciseIds { get; init; }
}

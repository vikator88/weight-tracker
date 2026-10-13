using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Workouts the caller may see, plus the exercises they reference keyed by identifier.
/// </summary>
/// <remarks>
/// The Workout aggregate references exercises by identifier only, so the read joins them here.
/// </remarks>
public sealed record WorkoutListDTO(IReadOnlyList<Workout> Workouts, IReadOnlyDictionary<Id, Exercise> Exercises);

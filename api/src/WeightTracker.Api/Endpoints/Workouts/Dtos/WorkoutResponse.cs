using System.Text.Json.Serialization;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

public sealed record WorkoutResponse(
    Guid Id,
    DateOnly WorkoutDate,
    Guid UserId,
    Guid? TrainerId,
    string Status,
    IReadOnlyList<WorkoutExerciseResponse> Exercises)
{
    /// <param name="workout">Workout to project.</param>
    /// <param name="exercises">Exercises referenced by the workout, keyed by identifier.</param>
    public static WorkoutResponse From(Workout workout, IReadOnlyDictionary<Id, Exercise> exercises)
        => new(
            workout.Id.Value,
            DateOnly.FromDateTime(workout.WorkoutDate),
            workout.UserId.Value,
            workout.TrainerId?.Value,
            workout.Status.ToString(),
            workout.Exercises
                .Select(entry => WorkoutExerciseResponse.From(entry, exercises[entry.ExerciseId]))
                .ToList());
}

/// <summary>
/// A single workout as read by id. Entries reference the catalog exercise by <c>ExerciseId</c> only:
/// the client already holds the catalog, so no exercise data is embedded.
/// </summary>
public sealed record WorkoutDetailResponse(
    Guid Id,
    DateOnly WorkoutDate,
    Guid UserId,
    Guid? TrainerId,
    string Status,
    IReadOnlyList<WorkoutExerciseDetailResponse> Exercises)
{
    public static WorkoutDetailResponse From(Workout workout)
        => new(
            workout.Id.Value,
            DateOnly.FromDateTime(workout.WorkoutDate),
            workout.UserId.Value,
            workout.TrainerId?.Value,
            workout.Status.ToString(),
            workout.Exercises.Select(WorkoutExerciseDetailResponse.From).ToList());
}

/// <param name="Id">Identifier of this entry of the workout.</param>
/// <param name="ExerciseId">Identifier of the catalog exercise performed in this entry.</param>
/// <param name="SuperSeriesId">Superseries this entry belongs to, <c>null</c> when it is performed on its own.</param>
public sealed record WorkoutExerciseDetailResponse(
    Guid Id,
    Guid ExerciseId,
    string Effort,
    IReadOnlyList<SetResponse> Sets,
    int? SuperSeriesId)
{
    public static WorkoutExerciseDetailResponse From(WorkoutExercise entry)
        => new(
            entry.Id.Value,
            entry.ExerciseId.Value,
            entry.Effort.ToString(),
            entry.Sets.Select(SetResponse.From).ToList(),
            entry.SuperSeriesId?.Value);
}

/// <param name="Id">Identifier of this entry of the workout, used to address it (for example to group it in a superseries).</param>
/// <param name="ExerciseId">Identifier of the catalog exercise performed in this entry.</param>
/// <param name="SuperSeriesId">Superseries this entry belongs to, <c>null</c> when it is performed on its own.</param>
public sealed record WorkoutExerciseResponse(
    Guid Id,
    Guid ExerciseId,
    string Name,
    string BodyPart,
    string Effort,
    IReadOnlyList<SetResponse> Sets,
    int? SuperSeriesId)
{
    public static WorkoutExerciseResponse From(WorkoutExercise entry, Exercise exercise)
        => new(
            entry.Id.Value,
            exercise.Id.Value,
            exercise.Name.Value,
            exercise.BodyPart.ToString(),
            entry.Effort.ToString(),
            entry.Sets.Select(SetResponse.From).ToList(),
            entry.SuperSeriesId?.Value);
}

public sealed record SetResponse(int Count, SetTargetResponse Target)
{
    public static SetResponse From(SetPrescription prescription)
        => new(prescription.count, SetTargetResponse.From(prescription.target));
}

/// <summary>
/// Discriminated projection of <see cref="SetTarget"/>.
/// </summary>
public sealed record SetTargetResponse(string Type)
{
    /// <summary>Omitted entirely for <c>MaxReps</c>, which carries no value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Value { get; init; }

    /// <summary>Upper bound of a <c>RepsRange</c>, whose lower bound is <see cref="Value"/>. Omitted for every other type.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxValue { get; init; }

    public static SetTargetResponse From(SetTarget target) => target switch
    {
        SetTarget.Reps reps => new SetTargetResponse(SetTargetTypes.Reps) { Value = reps.value },
        SetTarget.Duration duration => new SetTargetResponse(SetTargetTypes.Duration) { Value = duration.value },
        SetTarget.RepsRange range => new SetTargetResponse(SetTargetTypes.RepsRange) { Value = range.min, MaxValue = range.max },
        _ => new SetTargetResponse(SetTargetTypes.MaxReps),
    };
}

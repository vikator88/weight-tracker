using System.Text.Json.Serialization;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts.Dtos;

public sealed record WorkoutResponse(
    Guid Id,
    DateOnly WorkoutDate,
    Guid UserId,
    Guid? TrainerId,
    IReadOnlyList<WorkoutExerciseResponse> Exercises)
{
    public static WorkoutResponse From(Workout workout)
        => new(
            workout.Id.Value,
            DateOnly.FromDateTime(workout.WorkoutDate),
            workout.UserId.Value,
            workout.TrainerId?.Value,
            workout.Exercises
                .Select(entry => WorkoutExerciseResponse.From(entry.Key, entry.Value))
                .ToList());
}

public sealed record WorkoutExerciseResponse(
    Guid ExerciseId,
    string Name,
    string BodyPart,
    IReadOnlyList<SetResponse> Sets)
{
    public static WorkoutExerciseResponse From(
        Domain.Exercises.Exercise exercise, IReadOnlyList<SetPrescription> prescriptions)
        => new(
            exercise.Id.Value,
            exercise.Name,
            exercise.BodyPart.ToString(),
            prescriptions.Select(SetResponse.From).ToList());
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

    public static SetTargetResponse From(SetTarget target) => target switch
    {
        SetTarget.Reps reps => new SetTargetResponse("Reps") { Value = reps.value },
        SetTarget.Duration duration => new SetTargetResponse("Duration") { Value = duration.value },
        _ => new SetTargetResponse("MaxReps"),
    };
}

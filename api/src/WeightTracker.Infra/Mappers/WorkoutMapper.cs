using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

/// <summary>
/// Maps the Workout aggregate across the three tables it spans:
/// <c>workouts</c>, <c>workout_exercises</c> and <c>workout_exercise_sets</c>.
/// </summary>
public static class WorkoutMapper
{
    private const string RepsTarget = "Reps";
    private const string DurationTarget = "Duration";
    private const string MaxRepsTarget = "MaxReps";

    public static WorkoutEntity MapToEntity(Workout workout)
    {
        var entity = new WorkoutEntity
        {
            Id = workout.Id.Value,
            UserId = workout.UserId.Value,
            TrainerId = workout.TrainerId?.Value,
            WorkoutDate = DateOnly.FromDateTime(workout.WorkoutDate),
        };

        var exercisePosition = 0;

        foreach (var entry in workout.Exercises)
        {
            var workoutExercise = new WorkoutExerciseEntity
            {
                Id = entry.Id.Value,
                WorkoutId = entity.Id,
                ExerciseId = entry.ExerciseId.Value,
                Position = exercisePosition++,
                Effort = (int)entry.Effort,
                SuperSeriesId = entry.SuperSeriesId?.Value,
            };

            var setPosition = 0;

            foreach (var prescription in entry.Sets)
            {
                var (targetType, targetValue) = MapTargetToColumns(prescription.target);

                workoutExercise.Sets.Add(new WorkoutExerciseSetEntity
                {
                    Id = Id.New().Value,
                    WorkoutExerciseId = workoutExercise.Id,
                    Position = setPosition++,
                    Count = prescription.count,
                    TargetType = targetType,
                    TargetValue = targetValue,
                });
            }

            entity.WorkoutExercises.Add(workoutExercise);
        }

        return entity;
    }

    /// <summary>
    /// Restores the aggregate.
    /// </summary>
    /// <param name="entity">Workout rows, with its exercises and sets loaded.</param>
    public static Workout MapToDomain(WorkoutEntity entity)
    {
        try
        {
            var exercises = entity.WorkoutExercises
                .OrderBy(item => item.Position)
                .Select(item => WorkoutExercise.Rehydrate(
                    Id.From(item.Id),
                    Id.From(item.ExerciseId),
                    MapEffort(item.Effort),
                    item.Sets
                        .OrderBy(set => set.Position)
                        .Select(set => new SetPrescription(set.Count, MapTargetToDomain(set.TargetType, set.TargetValue))),
                    item.SuperSeriesId is null ? null : SuperSeriesId.From(item.SuperSeriesId.Value)))
                .ToList();

            return Workout.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.UserId),
                entity.TrainerId is null ? null : Id.From(entity.TrainerId.Value),
                entity.WorkoutDate.ToDateTime(TimeOnly.MinValue),
                exercises);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored workout '{entity.Id}' could not be restored", exception);
        }
    }

    private static (string TargetType, int? TargetValue) MapTargetToColumns(SetTarget target) => target switch
    {
        SetTarget.Reps reps => (RepsTarget, reps.value),
        SetTarget.Duration duration => (DurationTarget, duration.value),
        SetTarget.MaxReps => (MaxRepsTarget, (int?)null),
        _ => throw new PersistenceMappingException($"'{target.GetType().Name}' is not a known set target"),
    };

    private static SetTarget MapTargetToDomain(string targetType, int? targetValue) => targetType switch
    {
        RepsTarget => new SetTarget.Reps(RequireValue(targetType, targetValue)),
        DurationTarget => new SetTarget.Duration(RequireValue(targetType, targetValue)),
        MaxRepsTarget => new SetTarget.MaxReps(),
        _ => throw new PersistenceMappingException($"'{targetType}' is not a known set target"),
    };

    private static Effort MapEffort(int effort)
    {
        if (Enum.IsDefined(typeof(Effort), effort) == false)
            throw new PersistenceMappingException($"'{effort}' is not a known effort");

        return (Effort)effort;
    }

    private static int RequireValue(string targetType, int? targetValue)
        => targetValue ?? throw new PersistenceMappingException($"Set target '{targetType}' requires a value");
}

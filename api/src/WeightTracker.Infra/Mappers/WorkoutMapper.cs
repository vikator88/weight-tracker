using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
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

        foreach (var (exercise, prescriptions) in workout.Exercises)
        {
            var workoutExercise = new WorkoutExerciseEntity
            {
                Id = Id.New().Value,
                WorkoutId = entity.Id,
                ExerciseId = exercise.Id.Value,
                Position = exercisePosition++,
            };

            var setPosition = 0;

            foreach (var prescription in prescriptions)
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
    /// <param name="exercises">
    /// Exercises referenced by the workout, keyed by identifier. They are loaded separately
    /// because Exercise is its own aggregate root and is referenced here by identifier only.
    /// </param>
    public static Workout MapToDomain(WorkoutEntity entity, IReadOnlyDictionary<Guid, ExerciseEntity> exercises)
    {
        var prescriptions = new Dictionary<Exercise, List<SetPrescription>>();

        foreach (var workoutExercise in entity.WorkoutExercises.OrderBy(item => item.Position))
        {
            if (exercises.TryGetValue(workoutExercise.ExerciseId, out var exerciseEntity) == false)
                throw new PersistenceMappingException(
                    $"Workout '{entity.Id}' references unknown exercise '{workoutExercise.ExerciseId}'");

            var sets = workoutExercise.Sets
                .OrderBy(set => set.Position)
                .Select(set => new SetPrescription(set.Count, MapTargetToDomain(set.TargetType, set.TargetValue)))
                .ToList();

            prescriptions.Add(ExerciseMapper.MapToDomain(exerciseEntity), sets);
        }

        try
        {
            return Workout.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.UserId),
                entity.TrainerId is null ? null : Id.From(entity.TrainerId.Value),
                entity.WorkoutDate.ToDateTime(TimeOnly.MinValue),
                prescriptions);
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

    private static int RequireValue(string targetType, int? targetValue)
        => targetValue ?? throw new PersistenceMappingException($"Set target '{targetType}' requires a value");
}

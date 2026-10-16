using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

public sealed class Workout : AggregateRoot
{

    private DateTime _workoutDate;

    private List<WorkoutExercise> _exercises;

    /// <summary>
    /// Owner of the workout. Immutable once the workout exists.
    /// </summary>
    public Id UserId { get; private set; }

    /// <summary>
    /// Trainer who prescribed the workout, when somebody other than the owner created it.
    /// </summary>
    public Id? TrainerId { get; private set; }

    public DateTime WorkoutDate => _workoutDate;

    /// <summary>
    /// Snapshot of the exercises of this workout, in the order they must be performed.
    /// The list is copied so callers cannot mutate the aggregate through it.
    /// </summary>
    public IReadOnlyList<WorkoutExercise> Exercises => _exercises.ToList();

    private Workout(Id userId, Id? trainerId, DateTime workoutDate)
    {
        this.Id = Id.New();
        UserId = userId;
        TrainerId = trainerId;
        _workoutDate = workoutDate.Date;
        _exercises = new List<WorkoutExercise>();
    }

    /// <summary>
    /// Creates a workout for a user, optionally prescribed by a trainer.
    /// </summary>
    /// <exception cref="TrainerCannotBeWorkoutOwnerException">When the trainer is the owner.</exception>
    public static Workout Create(Id userId, Id? trainerId, DateTime workoutDate)
    {
        if (trainerId is not null && trainerId == userId)
            throw new TrainerCannotBeWorkoutOwnerException(userId);

        return new Workout(userId, trainerId, workoutDate)
        {
            IsNew = true
        };
    }

    /// <exception cref="ExerciseAlreadyInWorkoutException">When two entries share the same exercise.</exception>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When a stored superseries has a single member.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the members of a stored superseries have different set totals.</exception>
    public static Workout Rehydrate(
        Id id,
        Id userId,
        Id? trainerId,
        DateTime workoutdate,
        IEnumerable<WorkoutExercise> exercises
    )
    {
        var entries = exercises.ToList();

        var duplicate = entries
            .GroupBy(entry => entry.ExerciseId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new ExerciseAlreadyInWorkoutException(duplicate.Key, id);

        foreach (var group in entries.Where(entry => entry.SuperSeriesId is not null).GroupBy(entry => entry.SuperSeriesId))
            EnsureValidSuperSeries(id, group.ToList());

        return new Workout(userId, trainerId, workoutdate)
        {
            Id = id,
            IsNew = false,
            _exercises = entries,
        };
    }

    /// <summary>
    /// Appends an exercise to the end of the workout with a given effort and ordered sets.
    /// </summary>
    /// <param name="exerciseId">Exercise to include into the workout</param>
    /// <param name="effort">How hard the exercise must be performed</param>
    /// <param name="sets">Ordered config of number of sets x reps (time, max)</param>
    /// <exception cref="ExerciseAlreadyInWorkoutException">When the exercise is already in the workout.</exception>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    public void AddExercise(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)
    {
        if (_exercises.Any(entry => entry.ExerciseId == exerciseId))
            throw new ExerciseAlreadyInWorkoutException(exerciseId, this.Id);

        _exercises.Add(WorkoutExercise.Create(exerciseId, effort, sets));
    }

    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    public void RemoveExercise(Id exerciseId)
    {
        var entry = _exercises.FirstOrDefault(item => item.ExerciseId == exerciseId)
            ?? throw new ExerciseNotInWorkoutException(exerciseId, this.Id);

        _exercises.Remove(entry);

        // A superseries cannot be left with a single exercise
        if (entry.SuperSeriesId is not null)
        {
            var remaining = _exercises.Where(item => item.SuperSeriesId == entry.SuperSeriesId).ToList();

            if (remaining.Count == 1)
                remaining[0].LeaveSuperSeries();
        }
    }

    /// <summary>
    /// Groups exercises of this workout into a superseries: the first set of every exercise is done
    /// back to back, then rest, then the next set of every exercise.
    /// </summary>
    /// <param name="exerciseIds">Exercises of this workout to group</param>
    /// <returns>The identifier of the new superseries, unique inside this workout.</returns>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When fewer than two exercises are given.</exception>
    /// <exception cref="DuplicateExerciseInSuperSeriesException">When the same exercise is given twice.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When an exercise is not in the workout.</exception>
    /// <exception cref="ExerciseAlreadyInSuperSeriesException">When an exercise already belongs to a superseries.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the exercises have different numbers of sets.</exception>
    public SuperSeriesId CreateSuperSeries(IEnumerable<Id> exerciseIds)
    {
        var requested = exerciseIds.ToList();

        if (requested.Count < 2)
            throw new SuperSeriesRequiresTwoExercisesException(requested.Count);

        var duplicate = requested.GroupBy(exerciseId => exerciseId).FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new DuplicateExerciseInSuperSeriesException(duplicate.Key);

        var members = requested
            .Select(exerciseId => _exercises.FirstOrDefault(item => item.ExerciseId == exerciseId)
                ?? throw new ExerciseNotInWorkoutException(exerciseId, this.Id))
            .ToList();

        var alreadyGrouped = members.FirstOrDefault(member => member.SuperSeriesId is not null);

        if (alreadyGrouped is not null)
            throw new ExerciseAlreadyInSuperSeriesException(alreadyGrouped.ExerciseId, this.Id);

        if (members.Select(member => member.TotalSets).Distinct().Count() > 1)
            throw new SuperSeriesSetCountMismatchException(this.Id);

        var nextValue = _exercises.Where(item => item.SuperSeriesId is not null).Select(item => item.SuperSeriesId!.Value).DefaultIfEmpty(0).Max() + 1;
        var superSeriesId = SuperSeriesId.From(nextValue);

        foreach (var member in members)
            member.JoinSuperSeries(superSeriesId);

        return superSeriesId;
    }

    /// <summary>
    /// Ungroups a superseries. Its exercises stay in the workout.
    /// </summary>
    /// <exception cref="SuperSeriesNotFoundException">When no exercise of the workout belongs to it.</exception>
    public void BreakSuperSeries(SuperSeriesId superSeriesId)
    {
        var members = _exercises.Where(item => item.SuperSeriesId == superSeriesId).ToList();

        if (members.Count == 0)
            throw new SuperSeriesNotFoundException(superSeriesId, this.Id);

        foreach (var member in members)
            member.LeaveSuperSeries();
    }

    private static void EnsureValidSuperSeries(Id workoutId, IReadOnlyList<WorkoutExercise> members)
    {
        if (members.Count < 2)
            throw new SuperSeriesRequiresTwoExercisesException(members.Count);

        if (members.Select(member => member.TotalSets).Distinct().Count() > 1)
            throw new SuperSeriesSetCountMismatchException(workoutId);
    }
}

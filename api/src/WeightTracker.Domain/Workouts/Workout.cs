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
    }
}

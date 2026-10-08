using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Domain.Workouts;

public sealed class Workout : AggregateRoot
{

    private DateTime _workoutDate;

    private Dictionary<Exercise, List<SetPrescription>> _exercises;

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
    /// Snapshot of the prescriptions of this workout. Both the dictionary and the
    /// set lists are copied so callers cannot mutate the aggregate through it.
    /// </summary>
    public IReadOnlyDictionary<Exercise, IReadOnlyList<SetPrescription>> Exercises
        => _exercises.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<SetPrescription>)entry.Value.ToList());

    private Workout(Id userId, Id? trainerId, DateTime workoutDate)
    {
        this.Id = Id.New();
        UserId = userId;
        TrainerId = trainerId;
        _workoutDate = workoutDate.Date;
        _exercises = new Dictionary<Exercise, List<SetPrescription>>();
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

    public static Workout Rehydrate(
        Id id,
        Id userId,
        Id? trainerId,
        DateTime workoutdate,
        Dictionary<Exercise, List<SetPrescription>> exercises
    )
    {
        return new Workout(userId, trainerId, workoutdate)
        {
            Id = id,
            IsNew = false,
            _exercises = exercises,
        };
    }

    /// <summary>
    /// Add a exercise to the workout with a given config of sets
    /// </summary>
    /// <param name="exercise">Exercise to include into the workout</param>
    /// <param name="prescription">Config of number of sets x reps (time, max)</param>
    public void AddExercise(Exercise exercise, IEnumerable<SetPrescription> prescription)
    {
        if (_exercises.ContainsKey(exercise))
            _exercises[exercise].AddRange(prescription);
        else
            _exercises.Add(exercise, prescription.ToList());
    }

    public void RemoveExercise(Exercise exercise)
    {
        if (_exercises.ContainsKey(exercise) == false)
            throw new ExerciseNotInWorkoutException(exercise.Name, this.Id);

        _exercises.Remove(exercise);
    }
}

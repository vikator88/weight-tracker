using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// One exercise inside a workout: which exercise, how hard, and the ordered sets to perform.
/// </summary>
/// <remarks>
/// Entity of the Workout aggregate. It is only created, loaded and saved through <see cref="Workout"/>.
/// The Exercise aggregate is referenced by identifier only.
/// </remarks>
public sealed class WorkoutExercise : Entity
{
    private readonly List<SetPrescription> _sets;

    /// <summary>
    /// Exercise performed in this entry. Immutable once the entry exists.
    /// </summary>
    public Id ExerciseId { get; }

    public Effort Effort { get; }

    /// <summary>
    /// Snapshot of the prescriptions, in the order they must be performed.
    /// </summary>
    public IReadOnlyList<SetPrescription> Sets => _sets.ToList();

    private WorkoutExercise(Id id, Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets, bool isNew)
    {
        if (Enum.IsDefined(effort) == false)
            throw new InvalidEffortException((int)effort);

        this.Id = id;
        this.IsNew = isNew;
        ExerciseId = exerciseId;
        Effort = effort;
        _sets = sets.ToList();
    }

    /// <summary>
    /// Creates an entry that does not exist in the system yet.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    public static WorkoutExercise Create(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)
        => new(Id.New(), exerciseId, effort, sets, isNew: true);

    /// <summary>
    /// Restores an entry already stored in the system.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    public static WorkoutExercise Rehydrate(Id id, Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)
        => new(id, exerciseId, effort, sets, isNew: false);
}

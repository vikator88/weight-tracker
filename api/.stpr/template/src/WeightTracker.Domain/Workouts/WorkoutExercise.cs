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

    public Effort Effort { get; private set; }

    /// <summary>
    /// Snapshot of the prescriptions, in the order they must be performed.
    /// </summary>
    public IReadOnlyList<SetPrescription> Sets => _sets.ToList();

    /// <summary>
    /// Total number of sets to perform: the sum of the counts of every prescription.
    /// </summary>
    public int TotalSets => _sets.Sum(set => set.count);

    /// <summary>
    /// Superseries this entry belongs to, null when it is performed on its own.
    /// Only <see cref="Workout"/> changes it, because the grouping rules span several entries.
    /// </summary>
    public SuperSeriesId? SuperSeriesId { get; private set; }

    private WorkoutExercise(Id id, Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets, SuperSeriesId? superSeriesId, bool isNew)
    {
        var prescriptions = EnsureValid(effort, sets);

        this.Id = id;
        this.IsNew = isNew;
        ExerciseId = exerciseId;
        Effort = effort;
        _sets = prescriptions;
        SuperSeriesId = superSeriesId;
    }

    /// <summary>
    /// Creates an entry that does not exist in the system yet.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    public static WorkoutExercise Create(Id exerciseId, Effort effort, IEnumerable<SetPrescription> sets)
        => new(Id.New(), exerciseId, effort, sets, superSeriesId: null, isNew: true);

    /// <summary>
    /// Restores an entry already stored in the system.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    public static WorkoutExercise Rehydrate(
        Id id,
        Id exerciseId,
        Effort effort,
        IEnumerable<SetPrescription> sets,
        SuperSeriesId? superSeriesId = null)
        => new(id, exerciseId, effort, sets, superSeriesId, isNew: false);

    /// <summary>
    /// Replaces the effort and every set prescription. Nothing changes when the new values are invalid.
    /// Only <see cref="Workout"/> calls it, because changing the total of sets can break a superseries.
    /// </summary>
    /// <exception cref="InvalidEffortException">When the effort is not a defined value.</exception>
    /// <exception cref="WorkoutExerciseRequiresSetsException">When no set prescription is given.</exception>
    internal void Replace(Effort effort, IEnumerable<SetPrescription> sets)
    {
        var prescriptions = EnsureValid(effort, sets);

        Effort = effort;
        _sets.Clear();
        _sets.AddRange(prescriptions);
    }

    private static List<SetPrescription> EnsureValid(Effort effort, IEnumerable<SetPrescription> sets)
    {
        if (Enum.IsDefined(effort) == false)
            throw new InvalidEffortException((int)effort);

        var prescriptions = sets.ToList();

        if (prescriptions.Count == 0)
            throw new WorkoutExerciseRequiresSetsException();

        return prescriptions;
    }

    internal void JoinSuperSeries(SuperSeriesId superSeriesId) => SuperSeriesId = superSeriesId;

    internal void LeaveSuperSeries() => SuperSeriesId = null;
}

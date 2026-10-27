using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Reads the exercise catalog. It is read-only: exercises are maintained in the database, never written here.
/// </summary>
public interface IExerciseRepository
{

    public Task<IEnumerable<Exercise>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Exercises with the given identifiers. Unknown ids are ignored; empty input returns empty.
    /// </summary>
    public Task<IEnumerable<Exercise>> GetAllByIds(IEnumerable<Id> exerciseIds, CancellationToken cancellationToken);

    public Task<Exercise?> GetById(Id exerciseId, CancellationToken cancellationToken);

}

using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.Interfaces;

public interface IExerciseRepository
{

    public Task<IEnumerable<Exercise>> GetAll(CancellationToken cancellationToken);

    public Task<Exercise?> GetById(Id exerciseId, CancellationToken cancellationToken);

    public Task Save(Exercise exercise, CancellationToken cancellationToken);

    /// <summary>
    /// Stages the removal of an exercise. Nothing is removed until the unit of work commits.
    /// </summary>
    public Task Delete(Exercise exercise, CancellationToken cancellationToken);

}

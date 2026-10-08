using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.Interfaces;

public interface IExerciseRepository
{

    public Task<IEnumerable<Exercise>> GetAll(CancellationToken cancellationToken);

    public Task<Exercise?> GetById(Id exerciseId, CancellationToken cancellationToken);

    public Task Save(Exercise exercise, CancellationToken cancellationToken);

}

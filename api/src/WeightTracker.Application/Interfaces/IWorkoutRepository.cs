using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Interfaces;

public interface IWorkoutRepository
{

    public Task<IEnumerable<Workout>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Workouts owned by the given user.
    /// </summary>
    public Task<IEnumerable<Workout>> GetAllByUserId(Id userId, CancellationToken cancellationToken);

    /// <summary>
    /// Workouts owned by the given user plus the ones they prescribed as a trainer.
    /// </summary>
    public Task<IEnumerable<Workout>> GetAllByUserOrTrainerId(Id id, CancellationToken cancellationToken);

    public Task<Workout?> GetById(Id workoutId, CancellationToken cancellationToken);

    public Task Save(Workout workout, CancellationToken cancellationToken);
}

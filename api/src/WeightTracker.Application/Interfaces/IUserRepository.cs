using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Interfaces;

public interface IUserRepository
{

    public Task<User?> GetById(Id userId, CancellationToken cancellationToken);

    /// <summary>
    /// Looks a user up by their unique business identifier.
    /// </summary>
    public Task<User?> GetByEmail(Email email, CancellationToken cancellationToken);

    /// <summary>
    /// Uniqueness check for the email business identifier, to run before persisting a user.
    /// </summary>
    public Task<bool> ExistsByEmail(Email email, CancellationToken cancellationToken);

    /// <summary>
    /// True when the user exists and is allowed to be assigned as the trainer of a workout.
    /// </summary>
    public Task<bool> IsTrainerOrAdmin(Id userId, CancellationToken cancellationToken);

    public Task Save(User user, CancellationToken cancellationToken);

}

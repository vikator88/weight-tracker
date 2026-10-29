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
    /// Every stored user, whatever their role, ordered by email ascending.
    /// </summary>
    public Task<IEnumerable<User>> GetAll(CancellationToken cancellationToken);

    public Task Save(User user, CancellationToken cancellationToken);

}

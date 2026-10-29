using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

public class DeactivateUserUseCase
{
    private const string Operation = "PUT /users/{id}/deactivate";

    private IUserRepository _users;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public DeactivateUserUseCase(
        IUserRepository users,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Switches another user's account off. Only an administrator may do it, and never on themselves.
    /// </summary>
    /// <returns>The deactivated user.</returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is not an administrator, or targets themselves.</exception>
    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    /// <exception cref="UserAlreadyDeactivatedException">When the user is already deactivated.</exception>
    public async Task<User> Execute(Id callerId, Role callerRole, Id userId, CancellationToken cancellationToken)
    {
        if (callerRole != Role.ADMIN)
            throw new UnauthorizedOperationException(Operation);

        // Checked before the lookup so an administrator can never lock themselves out
        if (userId == callerId)
            throw new UnauthorizedOperationException(Operation);

        var user = await _users.GetById(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        user.Deactivate(_clock.UtcNow);

        await _users.Save(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}

using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

public class ActivateUserUseCase
{
    private IUserRepository _users;

    private IUnitOfWork _unitOfWork;

    public ActivateUserUseCase(
        IUserRepository users,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Switches a deactivated account back on. Only an administrator may do it.
    /// </summary>
    /// <returns>The activated user.</returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is not an administrator.</exception>
    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    /// <exception cref="UserAlreadyActiveException">When the user is already active.</exception>
    public async Task<User> Execute(Role callerRole, Id userId, CancellationToken cancellationToken)
    {
        if (callerRole != Role.ADMIN)
            throw new UnauthorizedOperationException("PUT /users/{id}/activate");

        var user = await _users.GetById(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        user.Activate();

        await _users.Save(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}

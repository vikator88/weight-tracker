using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// Lists the users the caller is entitled to see.
/// </summary>
/// <remarks>
/// The caller is passed explicitly instead of through an ambient accessor, so the
/// Application layer stays free of request-scoped infrastructure.
/// </remarks>
public class GetAllUsersUseCase
{
    private IUserRepository _users;

    public GetAllUsersUseCase(IUserRepository users)
    {
        _users = users;
    }

    /// <summary>
    /// An administrator gets every user, whatever their role. <paramref name="callerId"/> is unused until trainers
    /// can list their own athletes, which will be scoped by it.
    /// </summary>
    /// <exception cref="TrainerAthletesNotImplementedException">When the caller is a trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is neither an administrator nor a trainer.</exception>
    public async Task<IEnumerable<User>> Execute(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        return callerRole switch
        {
            Role.ADMIN => await _users.GetAll(cancellationToken),
            Role.TRAINER => throw new TrainerAthletesNotImplementedException(),
            _ => throw new UnauthorizedOperationException("GET /users"),
        };
    }
}

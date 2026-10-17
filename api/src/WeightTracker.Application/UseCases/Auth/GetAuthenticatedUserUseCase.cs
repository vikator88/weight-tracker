using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.Auth;

/// <summary>
/// Resolves the user a validated access token was issued to, so protected routes only serve
/// callers that still exist.
/// </summary>
public class GetAuthenticatedUserUseCase
{
    private readonly IUserRepository _users;

    public GetAuthenticatedUserUseCase(IUserRepository users)
    {
        _users = users;
    }

    /// <exception cref="AuthenticatedUserNotFoundException">When no user has the given id.</exception>
    public async Task<AuthenticatedUser> Execute(Id userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetById(userId, cancellationToken)
            ?? throw new AuthenticatedUserNotFoundException();

        return new AuthenticatedUser(user.Id, user.Email, user.Name, user.Surname, user.Role);
    }
}

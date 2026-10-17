using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Auth;

/// <summary>
/// The caller of a protected request, resolved from a valid access token. It never carries the
/// password hash, so the API can hold it without reaching the <c>User</c> aggregate.
/// </summary>
/// <param name="Id">Identifier of the user, the token's subject.</param>
/// <param name="Email">Unique business identifier of the user.</param>
/// <param name="Name">Given name.</param>
/// <param name="Surname">Family name.</param>
/// <param name="Role">Role stored for the user.</param>
public sealed record AuthenticatedUser(Id Id, Email Email, PersonName Name, PersonName Surname, Role Role);

using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

/// <summary>
/// The user an administrator just created. It carries no authentication data.
/// </summary>
public sealed record CreatedUserResponse(Guid Id, string Email, string Name, string Surname, DateOnly DateBirth, string Role)
{
    public static CreatedUserResponse From(User user)
        => new(user.Id.Value, user.Email.Value, user.Name.Value, user.Surname.Value, user.DateBirth, user.Role.ToString());
}

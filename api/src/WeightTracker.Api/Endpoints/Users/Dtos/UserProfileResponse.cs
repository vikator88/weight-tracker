using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

public sealed record UserProfileResponse(Guid Id, string Email, string Name, string Surname, string Role)
{
    public static UserProfileResponse From(AuthenticatedUser user)
        => new(user.Id.Value, user.Email.Value, user.Name.Value, user.Surname.Value, user.Role.ToString());

    public static UserProfileResponse From(User user)
        => new(user.Id.Value, user.Email.Value, user.Name.Value, user.Surname.Value, user.Role.ToString());
}

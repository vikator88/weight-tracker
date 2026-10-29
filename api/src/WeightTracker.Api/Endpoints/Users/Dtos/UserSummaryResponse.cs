using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

/// <summary>
/// A user as an administrator sees them: the public profile plus the activation state.
/// </summary>
public sealed record UserSummaryResponse(
    Guid Id, string Email, string Name, string Surname, string Role, bool IsActive, DateTime? DeactivatedAtUtc)
{
    public static UserSummaryResponse From(User user)
        => new(
            user.Id.Value, user.Email.Value, user.Name.Value, user.Surname.Value, user.Role.ToString(),
            user.IsActive, user.DeactivatedAtUtc);
}

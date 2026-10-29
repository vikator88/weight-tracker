using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// A user an administrator creates on someone else's behalf, as received from the API. Name, surname and
/// date of birth are optional.
/// </summary>
public sealed record CreateUserDTO(
    string Email,
    string Password,
    Role Role,
    string? Name,
    string? Surname,
    DateOnly? DateBirth);

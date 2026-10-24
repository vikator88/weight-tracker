namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// The editable part of a user's profile, as received from the API.
/// </summary>
public sealed record UpdateProfileDTO(string Name, string Surname);

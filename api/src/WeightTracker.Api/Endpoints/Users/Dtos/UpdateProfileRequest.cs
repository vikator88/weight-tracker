using System.ComponentModel.DataAnnotations;
using WeightTracker.Application.UseCases.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

/// <summary>
/// Body of <c>PUT /users/me</c>. It carries only the fields a user may edit: any other member
/// sent by the client, such as <c>email</c> or <c>role</c>, is not bound. Only structural
/// validation lives here; the name rules are enforced by the domain.
/// </summary>
public sealed record UpdateProfileRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string Surname { get; init; } = string.Empty;

    public UpdateProfileDTO ToDto() => new(Name, Surname);
}

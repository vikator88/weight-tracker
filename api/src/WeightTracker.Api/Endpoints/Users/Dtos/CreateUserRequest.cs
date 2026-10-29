using System.ComponentModel.DataAnnotations;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

/// <summary>
/// Body of <c>POST /users</c>. Only structural validation lives here; the email, password and name rules
/// are enforced by the domain. <c>Role</c> is nullable so a missing role is rejected instead of
/// defaulting.
/// </summary>
public sealed record CreateUserRequest
{
    [Required]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    [EnumDataType(typeof(Role))]
    public Role? Role { get; init; }

    public string? Name { get; init; }

    public string? Surname { get; init; }

    public DateOnly? DateBirth { get; init; }

    public CreateUserDTO ToDto() => new(Email, Password, Role!.Value, Name, Surname, DateBirth);
}

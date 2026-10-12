using System.ComponentModel.DataAnnotations;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Auth.Dtos;

public sealed record SignupRequest
{
    [Required]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    [EnumDataType(typeof(Role))]
    public Role Role { get; init; } = Role.USER;
}

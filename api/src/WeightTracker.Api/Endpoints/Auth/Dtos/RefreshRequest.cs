using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.Auth.Dtos;

public sealed record RefreshRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

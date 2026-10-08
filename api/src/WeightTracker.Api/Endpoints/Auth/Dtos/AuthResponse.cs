using WeightTracker.Application.UseCases.Auth;

namespace WeightTracker.Api.Endpoints.Auth.Dtos;

/// <summary>
/// Credential pair returned by login and refresh.
/// </summary>
/// <param name="AccessToken">Serialized as <c>accessToken</c>.</param>
/// <param name="RefreshToken">Serialized as <c>refreshToken</c>.</param>
/// <param name="ExpiresIn">Serialized as <c>expiresIn</c>. Lifetime of the access token, in seconds.</param>
public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn)
{
    public static AuthResponse From(AuthResult result)
        => new(result.AccessToken, result.RefreshToken, result.ExpiresIn);
}

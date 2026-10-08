namespace WeightTracker.Application.UseCases.Auth;

/// <summary>
/// Credentials returned by the login and refresh operations.
/// </summary>
/// <param name="AccessToken">Signed access token.</param>
/// <param name="RefreshToken">Raw refresh token. Only its hash is stored.</param>
/// <param name="ExpiresIn">Lifetime of the access token, in seconds.</param>
public sealed record AuthResult(string AccessToken, string RefreshToken, int ExpiresIn);

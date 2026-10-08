using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Issues the credentials returned by the authentication endpoints.
/// </summary>
public interface ITokenService
{
    GeneratedAccessToken GenerateAccessToken(User user);

    GeneratedRefreshToken GenerateRefreshToken();

    /// <summary>
    /// Hashes a raw refresh token so it can be matched against the stored hash.
    /// </summary>
    string HashRefreshToken(string rawRefreshToken);
}

/// <param name="Value">Signed access token.</param>
/// <param name="ExpiresInSeconds">Lifetime of the access token, in seconds.</param>
public sealed record GeneratedAccessToken(string Value, int ExpiresInSeconds);

/// <param name="RawValue">Token handed to the caller. Never persisted.</param>
/// <param name="Hash">Digest persisted in place of the raw value.</param>
/// <param name="ExpiresAtUtc">Absolute expiry of the refresh token.</param>
public sealed record GeneratedRefreshToken(string RawValue, string Hash, DateTime ExpiresAtUtc);

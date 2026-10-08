namespace WeightTracker.Infra.Services;

/// <summary>
/// Bound from the <c>Jwt</c> section of the application configuration.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum signing key length, in bytes, accepted for HS256.</summary>
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Never committed outside the development configuration.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}

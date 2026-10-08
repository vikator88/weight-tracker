namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>refresh_tokens</c> table.</summary>
public class RefreshTokenEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Digest of the token. The raw value never reaches the database.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public DateTime CreatedAt { get; set; }
}

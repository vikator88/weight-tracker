using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

public static class RefreshTokenMapper
{
    public static RefreshTokenEntity MapToEntity(RefreshToken refreshToken)
    {
        return new RefreshTokenEntity
        {
            Id = refreshToken.Id.Value,
            UserId = refreshToken.UserId.Value,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = DateTime.SpecifyKind(refreshToken.ExpiresAt, DateTimeKind.Utc),
            RevokedAt = refreshToken.RevokedAt is null
                ? null
                : DateTime.SpecifyKind(refreshToken.RevokedAt.Value, DateTimeKind.Utc),
            ReplacedByTokenId = refreshToken.ReplacedByTokenId?.Value,
            CreatedAt = DateTime.SpecifyKind(refreshToken.CreatedAt, DateTimeKind.Utc),
        };
    }

    public static RefreshToken MapToDomain(RefreshTokenEntity entity)
    {
        try
        {
            return RefreshToken.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.UserId),
                entity.TokenHash,
                entity.ExpiresAt,
                entity.RevokedAt,
                entity.ReplacedByTokenId is null ? null : Id.From(entity.ReplacedByTokenId.Value),
                entity.CreatedAt);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored refresh token '{entity.Id}' could not be restored", exception);
        }
    }
}

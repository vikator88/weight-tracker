using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly WeightTrackerDbContext _context;

    public RefreshTokenRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenHash(string tokenHash, CancellationToken cancellationToken)
    {
        var entity = await _context.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        return entity is null ? null : RefreshTokenMapper.MapToDomain(entity);
    }

    public async Task<RefreshToken?> GetById(Id refreshTokenId, CancellationToken cancellationToken)
    {
        var entity = await _context.RefreshTokens.FirstOrDefaultAsync(token => token.Id == refreshTokenId.Value, cancellationToken);

        return entity is null ? null : RefreshTokenMapper.MapToDomain(entity);
    }

    public async Task Save(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        var entity = RefreshTokenMapper.MapToEntity(refreshToken);

        if (refreshToken.IsNew)
        {
            await _context.RefreshTokens.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await _context.RefreshTokens.FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            _context.RefreshTokens.Update(entity);
            return;
        }

        // Rotation only ever changes the revocation columns
        tracked.RevokedAt = entity.RevokedAt;
        tracked.ReplacedByTokenId = entity.ReplacedByTokenId;
    }
}

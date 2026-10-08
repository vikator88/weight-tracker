using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Interfaces;

public interface IRefreshTokenRepository
{

    /// <summary>
    /// Looks a token up by its stored hash. The raw token is never persisted.
    /// </summary>
    public Task<RefreshToken?> GetByTokenHash(string tokenHash, CancellationToken cancellationToken);

    public Task<RefreshToken?> GetById(Id refreshTokenId, CancellationToken cancellationToken);

    public Task Save(RefreshToken refreshToken, CancellationToken cancellationToken);

}

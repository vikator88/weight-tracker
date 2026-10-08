using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Auth;

namespace WeightTracker.Application.UseCases.Auth;

/// <summary>
/// Redeems a refresh token for a fresh credential pair.
/// </summary>
/// <remarks>
/// Rotation: the presented token is revoked and linked to its replacement, so replaying
/// it is rejected even while it is still inside its original validity window.
/// </remarks>
public class RefreshTokenUseCase
{
    private IUserRepository _users;
    private IRefreshTokenRepository _refreshTokens;
    private ITokenService _tokens;
    private IClock _clock;
    private IUnitOfWork _unitOfWork;

    public RefreshTokenUseCase(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokens,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <exception cref="InvalidRefreshTokenException">
    /// When the token is missing, unknown, expired or already revoked.
    /// </exception>
    /// <exception cref="UserNotFoundException">When the token points at a user that no longer exists.</exception>
    public async Task<AuthResult> Execute(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidRefreshTokenException();

        var tokenHash = _tokens.HashRefreshToken(refreshToken);

        var storedToken = await _refreshTokens.GetByTokenHash(tokenHash, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var now = _clock.UtcNow;

        if (storedToken.IsActive(now) == false)
            throw new InvalidRefreshTokenException();

        var user = await _users.GetById(storedToken.UserId, cancellationToken)
            ?? throw new UserNotFoundException(storedToken.UserId);

        var accessToken = _tokens.GenerateAccessToken(user);
        var generatedRefreshToken = _tokens.GenerateRefreshToken();

        var replacement = RefreshToken.Create(
            user.Id, generatedRefreshToken.Hash, generatedRefreshToken.ExpiresAtUtc, now);

        storedToken.Revoke(replacement.Id, now);

        await _refreshTokens.Save(replacement, cancellationToken);
        await _refreshTokens.Save(storedToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken.Value, generatedRefreshToken.RawValue, accessToken.ExpiresInSeconds);
    }
}

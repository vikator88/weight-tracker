using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exceptions;

public sealed class RefreshTokenAlreadyRevokedException : DomainException
{
    public RefreshTokenAlreadyRevokedException(Id refreshTokenId)
        : base($"Refresh token '{refreshTokenId}' was already revoked")
    { }
}

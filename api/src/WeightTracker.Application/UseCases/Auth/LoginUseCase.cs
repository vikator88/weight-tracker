using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Application.UseCases.Auth;

/// <summary>
/// Exchanges a set of credentials for an access token and a refresh token.
/// </summary>
public class LoginUseCase
{
    private IUserRepository _users;
    private IRefreshTokenRepository _refreshTokens;
    private IPasswordHasher _passwordHasher;
    private ITokenService _tokens;
    private IClock _clock;
    private IUnitOfWork _unitOfWork;

    public LoginUseCase(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokens,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <exception cref="InvalidCredentialsException">
    /// When the email is malformed, unknown, or the password does not match. The three
    /// cases are deliberately indistinguishable so the endpoint cannot be used to
    /// enumerate registered users.
    /// </exception>
    public async Task<AuthResult> Execute(string email, string password, CancellationToken cancellationToken)
    {
        Email parsedEmail;

        try
        {
            parsedEmail = Email.From(email);
        }
        catch (InvalidEmailException)
        {
            throw new InvalidCredentialsException();
        }

        var user = await _users.GetByEmail(parsedEmail, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (_passwordHasher.Verify(password, user.PasswordHash) == false)
            throw new InvalidCredentialsException();

        var accessToken = _tokens.GenerateAccessToken(user);
        var refreshToken = _tokens.GenerateRefreshToken();

        await _refreshTokens.Save(
            RefreshToken.Create(user.Id, refreshToken.Hash, refreshToken.ExpiresAtUtc, _clock.UtcNow), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken.Value, refreshToken.RawValue, accessToken.ExpiresInSeconds);
    }
}

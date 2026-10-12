using System.Reflection.Metadata;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Auth;

/// <summary>
///
/// </summary>
public class SignupUseCase
{
    private IUserRepository _users;

    private IRefreshTokenRepository _refreshTokens;

    private IPasswordHasher _passwordHasher;

    private ITokenService _tokens;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public SignupUseCase(
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
    /// <exception cref="AdminSignupNotAllowedException">
    /// When the requested role is <see cref="Role.ADMIN"/>. Checked before anything else.
    /// </exception>
    public async Task<AuthResult> Execute(
        string email,
        string password,
        Role userRole,
        CancellationToken cancellationToken
    )
    {
        if (userRole == Role.ADMIN)
            throw new AdminSignupNotAllowedException();

        Email parsedEmail;
        Password validatedPassword;

        try
        {
            parsedEmail = Email.From(email);
            validatedPassword = Password.From(password);
        }
        catch (InvalidEmailException)
        {
            throw new InvalidCredentialsException();
        }
        catch (InvalidPasswordException)
        {
            throw new PasswordNotStrongEnoughException();
        }

        var emailAlreadyExists = await _users.ExistsByEmail(parsedEmail, cancellationToken);

        if (emailAlreadyExists)
            throw new EmailAlreadyExistException(parsedEmail);

        var user = User.Create(
            parsedEmail,
            PersonName.Empty(),
            PersonName.Empty(),
            DateOnly.MinValue,
            userRole,
            _passwordHasher.Hash(validatedPassword),
            _clock.UtcNow
        );

        await _users.Save(user, cancellationToken);

        // Log in recently created user
        var accessToken = _tokens.GenerateAccessToken(user);
        var refreshToken = _tokens.GenerateRefreshToken();

        await _refreshTokens.Save(
            RefreshToken.Create(
                user.Id,
                refreshToken.Hash,
                refreshToken.ExpiresAtUtc,
                _clock.UtcNow
            ),
            cancellationToken
        );

        // Transactional
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            accessToken.Value,
            refreshToken.RawValue,
            accessToken.ExpiresInSeconds
        );
    }
}

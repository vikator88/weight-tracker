using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// Creates a user of any role on someone else's behalf. Unlike signup, it issues no tokens: the created
/// user logs in on their own.
/// </summary>
/// <remarks>
/// Who may call it is enforced by the endpoint, since it depends on the caller's role only.
/// </remarks>
public class CreateUserUseCase
{
    private IUserRepository _users;

    private IPasswordHasher _passwordHasher;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public CreateUserUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Validates every value first, so invalid input never reaches the database. Omitted names are stored
    /// empty and an omitted date of birth as <see cref="DateOnly.MinValue"/>, as signup does.
    /// </summary>
    /// <exception cref="InvalidEmailException">When the email is not a valid address.</exception>
    /// <exception cref="PasswordNotStrongEnoughException">When the password does not meet the strength rules.</exception>
    /// <exception cref="InvalidPersonNameException">When a given name or surname is not a valid person name.</exception>
    /// <exception cref="EmailAlreadyExistException">When a user with the same email already exists.</exception>
    public async Task<User> Execute(CreateUserDTO dto, CancellationToken cancellationToken)
    {
        var email = Email.From(dto.Email);

        Password password;

        try
        {
            password = Password.From(dto.Password);
        }
        catch (InvalidPasswordException)
        {
            throw new PasswordNotStrongEnoughException();
        }

        var name = dto.Name is null ? PersonName.Empty() : PersonName.From(dto.Name);
        var surname = dto.Surname is null ? PersonName.Empty() : PersonName.From(dto.Surname);

        if (await _users.ExistsByEmail(email, cancellationToken))
            throw new EmailAlreadyExistException(email);

        var user = User.Create(
            email,
            name,
            surname,
            dto.DateBirth ?? DateOnly.MinValue,
            dto.Role,
            _passwordHasher.Hash(password),
            _clock.UtcNow
        );

        await _users.Save(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}

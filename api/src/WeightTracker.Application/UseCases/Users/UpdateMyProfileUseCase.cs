using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

public class UpdateMyProfileUseCase
{
    private IUserRepository _users;

    private IUnitOfWork _unitOfWork;

    public UpdateMyProfileUseCase(
        IUserRepository users,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces the name and surname of the given user. Email and role are never changed.
    /// </summary>
    /// <exception cref="InvalidPersonNameException">When the name or surname is not a valid person name.</exception>
    /// <exception cref="AuthenticatedUserNotFoundException">When no user has the given id.</exception>
    public async Task<User> Execute(Id userId, UpdateProfileDTO dto, CancellationToken cancellationToken)
    {
        var name = PersonName.From(dto.Name);
        var surname = PersonName.From(dto.Surname);

        var user = await _users.GetById(userId, cancellationToken)
            ?? throw new AuthenticatedUserNotFoundException();

        user.UpdateProfile(name, surname);

        await _users.Save(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user;
    }
}

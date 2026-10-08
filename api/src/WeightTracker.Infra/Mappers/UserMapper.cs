using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

public static class UserMapper
{
    public static UserEntity MapToEntity(User user)
    {
        return new UserEntity
        {
            Id = user.Id.Value,
            Email = user.Email.Value,
            Name = user.Name.Value,
            Surname = user.Surname.Value,
            DateBirth = user.DateBirth,
            Role = (int)user.Role,
            PasswordHash = user.PasswordHash.Value,
            CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc),
        };
    }

    public static User MapToDomain(UserEntity entity)
    {
        try
        {
            return User.Rehydrate(
                Id.From(entity.Id),
                Email.From(entity.Email),
                PersonName.From(entity.Name),
                PersonName.From(entity.Surname),
                entity.DateBirth,
                MapRole(entity.Role),
                PasswordHash.From(entity.PasswordHash),
                entity.CreatedAt);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored user '{entity.Id}' could not be restored", exception);
        }
    }

    private static Role MapRole(int role)
    {
        if (Enum.IsDefined(typeof(Role), role) == false)
            throw new PersistenceMappingException($"'{role}' is not a known user role");

        return (Role)role;
    }
}

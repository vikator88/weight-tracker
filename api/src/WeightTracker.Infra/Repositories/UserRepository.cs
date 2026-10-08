using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class UserRepository : IUserRepository
{
    private readonly WeightTrackerDbContext _context;

    public UserRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetById(Id userId, CancellationToken cancellationToken)
    {
        var entity = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId.Value, cancellationToken);

        return entity is null ? null : UserMapper.MapToDomain(entity);
    }

    public async Task<User?> GetByEmail(Email email, CancellationToken cancellationToken)
    {
        var entity = await _context.Users.FirstOrDefaultAsync(user => user.Email == email.Value, cancellationToken);

        return entity is null ? null : UserMapper.MapToDomain(entity);
    }

    public Task<bool> ExistsByEmail(Email email, CancellationToken cancellationToken)
        => _context.Users.AnyAsync(user => user.Email == email.Value, cancellationToken);

    public Task<bool> IsTrainerOrAdmin(Id userId, CancellationToken cancellationToken)
        => _context.Users.AnyAsync(user =>
            user.Id == userId.Value
            && (user.Role == (int)Role.TRAINER || user.Role == (int)Role.ADMIN), cancellationToken);

    public async Task Save(User user, CancellationToken cancellationToken)
    {
        var entity = UserMapper.MapToEntity(user);

        if (user.IsNew)
        {
            await _context.Users.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await _context.Users.FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            _context.Users.Update(entity);
            return;
        }

        tracked.Email = entity.Email;
        tracked.Name = entity.Name;
        tracked.Surname = entity.Surname;
        tracked.DateBirth = entity.DateBirth;
        tracked.Role = entity.Role;
        tracked.PasswordHash = entity.PasswordHash;
    }
}

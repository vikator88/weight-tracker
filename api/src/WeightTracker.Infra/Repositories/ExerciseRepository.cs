using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class ExerciseRepository : IExerciseRepository
{
    private readonly WeightTrackerDbContext _context;

    public ExerciseRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Exercise>> GetAll(CancellationToken cancellationToken)
    {
        var entities = await _context.Exercises.AsNoTracking().ToListAsync(cancellationToken);

        return entities.Select(ExerciseMapper.MapToDomain).ToList();
    }

    public async Task<IEnumerable<Exercise>> GetAllByIds(IEnumerable<Id> exerciseIds, CancellationToken cancellationToken)
    {
        var ids = exerciseIds.Select(id => id.Value).Distinct().ToList();

        if (ids.Count == 0)
            return [];

        var entities = await _context.Exercises
            .AsNoTracking()
            .Where(exercise => ids.Contains(exercise.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(ExerciseMapper.MapToDomain).ToList();
    }

    public async Task<Exercise?> GetById(Id exerciseId, CancellationToken cancellationToken)
    {
        var entity = await _context.Exercises.FirstOrDefaultAsync(exercise => exercise.Id == exerciseId.Value, cancellationToken);

        return entity is null ? null : ExerciseMapper.MapToDomain(entity);
    }

    public async Task Delete(Exercise exercise, CancellationToken cancellationToken)
    {
        var tracked = await _context.Exercises.FirstOrDefaultAsync(stored => stored.Id == exercise.Id.Value, cancellationToken);

        _context.Exercises.Remove(tracked ?? ExerciseMapper.MapToEntity(exercise));
    }

    public async Task Save(Exercise exercise, CancellationToken cancellationToken)
    {
        var entity = ExerciseMapper.MapToEntity(exercise);

        if (exercise.IsNew)
        {
            await _context.Exercises.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await _context.Exercises.FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            _context.Exercises.Update(entity);
            return;
        }

        tracked.Name = entity.Name;
        tracked.VideoUrl = entity.VideoUrl;
        tracked.BodyPart = entity.BodyPart;
    }
}

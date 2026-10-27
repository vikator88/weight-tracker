using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Entities;
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
        var entities = await WithAggregateTables(_context.Exercises.AsNoTracking()).ToListAsync(cancellationToken);

        return entities.Select(ExerciseMapper.MapToDomain).ToList();
    }

    public async Task<IEnumerable<Exercise>> GetAllByIds(IEnumerable<Id> exerciseIds, CancellationToken cancellationToken)
    {
        var ids = exerciseIds.Select(id => id.Value).Distinct().ToList();

        if (ids.Count == 0)
            return [];

        var entities = await WithAggregateTables(_context.Exercises.AsNoTracking())
            .Where(exercise => ids.Contains(exercise.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(ExerciseMapper.MapToDomain).ToList();
    }

    public async Task<Exercise?> GetById(Id exerciseId, CancellationToken cancellationToken)
    {
        var entity = await WithAggregateTables(_context.Exercises.AsNoTracking())
            .FirstOrDefaultAsync(exercise => exercise.Id == exerciseId.Value, cancellationToken);

        return entity is null ? null : ExerciseMapper.MapToDomain(entity);
    }

    private static IQueryable<ExerciseEntity> WithAggregateTables(IQueryable<ExerciseEntity> exercises)
        => exercises.Include(exercise => exercise.Translations);
}

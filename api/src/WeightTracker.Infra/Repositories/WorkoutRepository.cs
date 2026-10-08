using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Repositories;

public class WorkoutRepository : IWorkoutRepository
{
    private readonly WeightTrackerDbContext _context;

    public WorkoutRepository(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public Task<IEnumerable<Workout>> GetAll(CancellationToken cancellationToken)
        => Query(workouts => workouts, cancellationToken);

    public Task<IEnumerable<Workout>> GetAllByUserId(Id userId, CancellationToken cancellationToken)
        => Query(workouts => workouts.Where(workout => workout.UserId == userId.Value), cancellationToken);

    public Task<IEnumerable<Workout>> GetAllByUserOrTrainerId(Id id, CancellationToken cancellationToken)
        => Query(workouts => workouts.Where(workout =>
            workout.UserId == id.Value || workout.TrainerId == id.Value), cancellationToken);

    public async Task<Workout?> GetById(Id workoutId, CancellationToken cancellationToken)
    {
        var entity = await WithAggregateTables(_context.Workouts.AsNoTracking())
            .FirstOrDefaultAsync(workout => workout.Id == workoutId.Value, cancellationToken);

        if (entity is null)
            return null;

        var exercises = await LoadReferencedExercises([entity], cancellationToken);

        return WorkoutMapper.MapToDomain(entity, exercises);
    }

    public async Task Save(Workout workout, CancellationToken cancellationToken)
    {
        var entity = WorkoutMapper.MapToEntity(workout);

        if (workout.IsNew)
        {
            await _context.Workouts.AddAsync(entity, cancellationToken);
            return;
        }

        var tracked = await WithAggregateTables(_context.Workouts)
            .FirstOrDefaultAsync(stored => stored.Id == entity.Id, cancellationToken);

        if (tracked is null)
        {
            await _context.Workouts.AddAsync(entity, cancellationToken);
            return;
        }

        tracked.UserId = entity.UserId;
        tracked.TrainerId = entity.TrainerId;
        tracked.WorkoutDate = entity.WorkoutDate;

        Reconcile(tracked, entity);
    }

    /// <summary>
    /// Brings the persisted children of the aggregate in line with the incoming state.
    /// </summary>
    /// <remarks>
    /// The children are reconciled rather than replaced wholesale. A workout exercise that
    /// survives the update keeps its row, which matters because <c>(workout_id, exercise_id)</c>
    /// is unique: deleting and reinserting the same pair in one transaction would collide.
    /// Removals are expressed by detaching from the navigation so EF cascades them as
    /// orphans, instead of mixing explicit deletes with collection reassignment.
    /// </remarks>
    private static void Reconcile(WorkoutEntity tracked, WorkoutEntity incoming)
    {
        var incomingByExercise = incoming.WorkoutExercises.ToDictionary(item => item.ExerciseId);

        var dropped = tracked.WorkoutExercises
            .Where(item => incomingByExercise.ContainsKey(item.ExerciseId) == false)
            .ToList();

        foreach (var item in dropped)
            tracked.WorkoutExercises.Remove(item);

        foreach (var incomingItem in incoming.WorkoutExercises)
        {
            var existing = tracked.WorkoutExercises
                .FirstOrDefault(item => item.ExerciseId == incomingItem.ExerciseId);

            if (existing is null)
            {
                tracked.WorkoutExercises.Add(incomingItem);
                continue;
            }

            existing.Position = incomingItem.Position;

            // Sets carry no identity outside the aggregate, so they are always replaced
            existing.Sets.Clear();

            foreach (var set in incomingItem.Sets)
            {
                set.WorkoutExerciseId = existing.Id;
                existing.Sets.Add(set);
            }
        }
    }

    private async Task<IEnumerable<Workout>> Query(
        Func<IQueryable<WorkoutEntity>, IQueryable<WorkoutEntity>> filter,
        CancellationToken cancellationToken)
    {
        var entities = await WithAggregateTables(filter(_context.Workouts.AsNoTracking()))
            .OrderBy(workout => workout.WorkoutDate)
            .ToListAsync(cancellationToken);

        var exercises = await LoadReferencedExercises(entities, cancellationToken);

        return entities.Select(entity => WorkoutMapper.MapToDomain(entity, exercises)).ToList();
    }

    private static IQueryable<WorkoutEntity> WithAggregateTables(IQueryable<WorkoutEntity> workouts)
        => workouts
            .Include(workout => workout.WorkoutExercises)
            .ThenInclude(workoutExercise => workoutExercise.Sets);

    /// <summary>
    /// Loads the exercises referenced by the given workouts.
    /// </summary>
    /// <remarks>
    /// Exercise is its own aggregate root, so the Workout tables reference it by identifier
    /// and never navigate into it. The rows are fetched in a single extra query instead.
    /// </remarks>
    private async Task<IReadOnlyDictionary<Guid, ExerciseEntity>> LoadReferencedExercises(
        IReadOnlyCollection<WorkoutEntity> workouts,
        CancellationToken cancellationToken)
    {
        var exerciseIds = workouts
            .SelectMany(workout => workout.WorkoutExercises)
            .Select(workoutExercise => workoutExercise.ExerciseId)
            .Distinct()
            .ToList();

        if (exerciseIds.Count == 0)
            return new Dictionary<Guid, ExerciseEntity>();

        var exercises = await _context.Exercises
            .AsNoTracking()
            .Where(exercise => exerciseIds.Contains(exercise.Id))
            .ToListAsync(cancellationToken);

        return exercises.ToDictionary(exercise => exercise.Id);
    }
}

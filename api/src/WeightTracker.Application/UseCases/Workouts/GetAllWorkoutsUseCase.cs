using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Lists the workouts the caller is entitled to see.
/// </summary>
/// <remarks>
/// The caller is passed explicitly instead of through an ambient accessor, so the
/// Application layer stays free of request-scoped infrastructure.
/// </remarks>
public class GetAllWorkoutsUseCase
{
    private IWorkoutRepository _workouts;

    private IExerciseRepository _exercises;

    public GetAllWorkoutsUseCase(
        IWorkoutRepository workouts,
        IExerciseRepository exercises
    )
    {
        _workouts = workouts;
        _exercises = exercises;
    }

    public async Task<WorkoutListDTO> Execute(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var workouts = (await ScopedWorkouts(callerId, callerRole, cancellationToken)).ToList();

        // Only exercises referenced by the scoped workouts, so the join never widens visibility
        var exerciseIds = workouts
            .SelectMany(workout => workout.Exercises)
            .Select(entry => entry.ExerciseId)
            .Distinct()
            .ToList();

        var exercises = await _exercises.GetAllByIds(exerciseIds, cancellationToken);

        return new WorkoutListDTO(workouts, exercises.ToDictionary(exercise => exercise.Id));
    }

    private async Task<IEnumerable<Workout>> ScopedWorkouts(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        return callerRole switch
        {
            Role.ADMIN => await _workouts.GetAll(cancellationToken),
            Role.TRAINER => PublishedToCaller(await _workouts.GetAllByUserOrTrainerId(callerId, cancellationToken), callerId),
            Role.USER => PublishedToCaller(await _workouts.GetAllByUserId(callerId, cancellationToken), callerId),
            _ => throw new UnauthorizedOperationException("GET /workouts"),
        };
    }

    /// <summary>
    /// Leaves out the prescribed drafts of which the caller is the athlete: their trainer has not published them yet.
    /// </summary>
    private static IEnumerable<Workout> PublishedToCaller(IEnumerable<Workout> workouts, Id callerId)
        => workouts.Where(workout => workout.IsHiddenFrom(callerId) == false);
}

using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class ReorderWorkoutExercisesUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public ReorderWorkoutExercisesUseCase(
        IWorkoutRepository workouts,
        WorkoutEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Puts the exercises of an existing workout in a new order.
    /// </summary>
    /// <param name="exerciseIds">Catalog exercises of the workout, every one exactly once, in the new order.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="WorkoutExerciseOrderMismatchException">When the ids are not exactly the exercises of the workout.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        IReadOnlyList<Id> exerciseIds,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, "PUT /workouts/{workoutId}/exercises/order");

        workout.ReorderExercises(exerciseIds);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}

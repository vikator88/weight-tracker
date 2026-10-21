using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Workouts;

public class RemoveWorkoutExerciseUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public RemoveWorkoutExerciseUseCase(
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
    /// Removes an exercise, and with it its set prescriptions, from an existing workout.
    /// </summary>
    /// <param name="exerciseId">Catalog exercise to remove, not the workout entry id.</param>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="ExerciseNotInWorkoutException">When the exercise is not in the workout.</exception>
    public async Task Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        Id exerciseId,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, "DELETE /workouts/{workoutId}/exercises/{exerciseId}");

        workout.RemoveExercise(exerciseId);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

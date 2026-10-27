using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class UpdateWorkoutUseCase
{
    private const string Operation = "PUT /workouts/{id}";

    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private WorkoutAssignmentValidator _assignment;

    private IUnitOfWork _unitOfWork;

    public UpdateWorkoutUseCase(
        IWorkoutRepository workouts,
        WorkoutEditAuthorizer authorizer,
        WorkoutAssignmentValidator assignment,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _authorizer = authorizer;
        _assignment = assignment;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces the name, description and day of a workout, and its owner and trainer. Changing the
    /// details is an edit; changing the owner or the trainer is reserved to admins. Status and exercises
    /// are untouched.
    /// </summary>
    /// <param name="userId">Owner the workout must have. A non-admin must send the current one.</param>
    /// <param name="trainerId">Trainer the workout must have, <c>null</c> for none. A non-admin must send the current one.</param>
    /// <param name="description">New description, <c>null</c> to clear it.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout, or a non-admin changes its owner or trainer.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="InvalidWorkoutAssignmentException">When an admin makes themselves the owner, or the new trainer cannot train.</exception>
    /// <exception cref="UserNotFoundException">When the new owner does not exist.</exception>
    /// <exception cref="TrainerCannotBeWorkoutOwnerException">When the trainer is the owner.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        WorkoutName name,
        WorkoutDescription? description,
        DateTime workoutDate,
        Id userId,
        Id? trainerId,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        var ownerChanged = userId != workout.UserId;
        var trainerChanged = trainerId != workout.TrainerId;

        // Checked before the edit rule so a non-admin who tries to reassign always gets 403, never 409
        if (callerRole != Role.ADMIN && (ownerChanged || trainerChanged))
            throw new UnauthorizedOperationException(Operation);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, Operation);

        if (ownerChanged || trainerChanged)
        {
            if (ownerChanged)
            {
                if (userId == callerId)
                    throw new InvalidWorkoutAssignmentException("Admins cannot make themselves the owner of a workout");

                await _assignment.EnsureOwnerExists(userId, cancellationToken);
            }

            if (trainerChanged && trainerId is not null)
                await _assignment.EnsureCanTrain(trainerId, cancellationToken);

            workout.Reassign(userId, trainerId);
        }

        workout.UpdateDetails(name, description, workoutDate);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}

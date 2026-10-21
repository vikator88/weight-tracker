using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides who may change an existing workout. Every use case that edits a workout goes through it,
/// so the rule cannot drift between endpoints.
/// </summary>
/// <remarks>
/// An admin may always edit. A prescribed workout may only be edited by its trainer, not even by its owner.
/// A workout without a trainer may be edited by its owner, whatever the owner's role.
/// Once a workout has started nobody but an admin may edit it.
/// </remarks>
public class WorkoutEditAuthorizer
{
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    public void EnsureCanEdit(Workout workout, Id callerId, Role callerRole, string operation)
    {
        if (CanEdit(workout, callerId, callerRole) == false)
            throw new UnauthorizedOperationException(operation);

        // Checked after the permission so a caller who may not edit never learns the status
        if (callerRole != Role.ADMIN && workout.IsLocked)
            throw new WorkoutLockedException(workout.Id, workout.Status);
    }

    private static bool CanEdit(Workout workout, Id callerId, Role callerRole)
    {
        if (callerRole == Role.ADMIN)
            return true;

        return workout.TrainerId is not null
            ? workout.TrainerId == callerId
            : workout.UserId == callerId;
    }
}

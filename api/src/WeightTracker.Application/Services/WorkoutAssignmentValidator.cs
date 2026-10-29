using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Checks the users a workout is assigned to. Shared by creating a workout and reassigning one,
/// so both apply the same rules to the owner and the trainer.
/// </summary>
public class WorkoutAssignmentValidator
{
    private IUserRepository _users;

    public WorkoutAssignmentValidator(IUserRepository users)
    {
        _users = users;
    }

    /// <returns>The owner, so callers can apply role rules of their own.</returns>
    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    public async Task<User> EnsureOwnerExists(Id ownerId, CancellationToken cancellationToken)
        => await _users.GetById(ownerId, cancellationToken)
            ?? throw new UserNotFoundException(ownerId);

    /// <exception cref="InvalidWorkoutAssignmentException">When the owner is deactivated.</exception>
    public void EnsureCanBeAssigned(User owner)
    {
        if (owner.CanBeAssignedWorkouts() == false)
            throw new InvalidWorkoutAssignmentException("A deactivated user cannot be assigned a workout");
    }

    /// <exception cref="InvalidWorkoutAssignmentException">
    /// When the user does not exist, is neither a trainer nor an admin, or is deactivated.
    /// </exception>
    public async Task EnsureCanTrain(Id trainerId, CancellationToken cancellationToken)
    {
        var trainer = await _users.GetById(trainerId, cancellationToken);

        if (trainer is null || trainer.CanTrain() == false)
            throw new InvalidWorkoutAssignmentException("The trainer of a workout must be a trainer or an admin");

        if (trainer.CanBeAssignedWorkouts() == false)
            throw new InvalidWorkoutAssignmentException("A deactivated user cannot be assigned as trainer");
    }
}

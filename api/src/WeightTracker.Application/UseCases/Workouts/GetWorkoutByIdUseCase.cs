using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Reads one workout, provided the caller is entitled to see it.
/// </summary>
/// <remarks>
/// The visibility rule mirrors <see cref="GetAllWorkoutsUseCase"/>: admins see every workout,
/// trainers the ones they own or prescribed, users the ones they own. Anyone but an admin is also
/// refused a prescribed draft of which they are the athlete, since the trainer has not published it yet.
/// </remarks>
public class GetWorkoutByIdUseCase
{
    private readonly IWorkoutRepository _workouts;

    public GetWorkoutByIdUseCase(IWorkoutRepository workouts)
    {
        _workouts = workouts;
    }

    public async Task<Workout> Execute(Id callerId, Role callerRole, Id workoutId, CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        var canSee = callerRole switch
        {
            Role.ADMIN => true,
            Role.TRAINER => workout.UserId == callerId || workout.TrainerId == callerId,
            Role.USER => workout.UserId == callerId,
            _ => false,
        };

        if (!canSee || (callerRole != Role.ADMIN && workout.IsHiddenFrom(callerId)))
        {
            throw new UnauthorizedOperationException($"GET /workouts/{workoutId}");
        }

        return workout;
    }
}

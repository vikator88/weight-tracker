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

    public GetAllWorkoutsUseCase(
        IWorkoutRepository workouts
    )
    {
        _workouts = workouts;
    }

    public async Task<IEnumerable<Workout>> Execute(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        return callerRole switch
        {
            Role.ADMIN => await _workouts.GetAll(cancellationToken),
            Role.TRAINER => await _workouts.GetAllByUserOrTrainerId(callerId, cancellationToken),
            Role.USER => await _workouts.GetAllByUserId(callerId, cancellationToken),
            _ => throw new UnauthorizedOperationException("GET /workouts"),
        };
    }
}

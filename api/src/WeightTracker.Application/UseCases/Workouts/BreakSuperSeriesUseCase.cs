using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class BreakSuperSeriesUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public BreakSuperSeriesUseCase(
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
    /// Ungroups a superseries of an existing workout. Its exercises stay in the workout.
    /// </summary>
    /// <exception cref="InvalidSuperSeriesIdException">When the superseries id is not a positive integer.</exception>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="SuperSeriesNotFoundException">When the workout has no such superseries.</exception>
    public async Task Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        int superSeriesId,
        CancellationToken cancellationToken)
    {
        var superSeries = SuperSeriesId.From(superSeriesId);

        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, "DELETE /workouts/{workoutId}/superseries/{superSeriesId}");

        workout.BreakSuperSeries(superSeries);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

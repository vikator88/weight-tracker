using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class CreateSuperSeriesUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public CreateSuperSeriesUseCase(
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
    /// Groups entries of an existing workout into a new superseries.
    /// </summary>
    /// <param name="workoutExerciseIds">Identifiers of the workout entries, not of the catalog exercises.</param>
    /// <returns>The identifier the workout assigned to the new superseries.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="SuperSeriesRequiresTwoExercisesException">When fewer than two entries are given.</exception>
    /// <exception cref="DuplicateExerciseInSuperSeriesException">When the same entry is given twice.</exception>
    /// <exception cref="WorkoutExerciseNotInWorkoutException">When an entry is not in the workout.</exception>
    /// <exception cref="ExerciseAlreadyInSuperSeriesException">When an entry already belongs to a superseries.</exception>
    /// <exception cref="SuperSeriesSetCountMismatchException">When the entries have different numbers of sets.</exception>
    public async Task<SuperSeriesId> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        IReadOnlyList<Id> workoutExerciseIds,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, "POST /workouts/{workoutId}/superseries");

        var superSeriesId = workout.CreateSuperSeries(workoutExerciseIds);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return superSeriesId;
    }
}

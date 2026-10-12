using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.Exercises;

public class DeleteExerciseUseCase
{
    private IExerciseRepository _exercises;

    private IWorkoutRepository _workouts;

    private IUnitOfWork _unitOfWork;

    public DeleteExerciseUseCase(
        IExerciseRepository exercises,
        IWorkoutRepository workouts,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _workouts = workouts;
        _unitOfWork = unitOfWork;
    }

    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id.</exception>
    /// <exception cref="ExerciseInUseException">When a workout still includes the exercise.</exception>
    public async Task Execute(Id id, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(id, cancellationToken);

        if (exercise is null)
            throw new ExerciseNotFoundException(id);

        // The rule spans two aggregates, so it lives here and not in either of them
        if (await _workouts.ExistsByExerciseId(id, cancellationToken))
            throw new ExerciseInUseException(id);

        await _exercises.Delete(exercise, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

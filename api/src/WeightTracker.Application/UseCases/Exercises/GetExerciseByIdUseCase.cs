using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.UseCases.Exercises;

public class GetExerciseByIdUseCase
{
    private IExerciseRepository _exercises;

    public GetExerciseByIdUseCase(
        IExerciseRepository exercises
    )
    {
        _exercises = exercises;
    }

    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id.</exception>
    public async Task<Exercise> Execute(Id id, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(id, cancellationToken);

        if (exercise is null)
            throw new ExerciseNotFoundException(id);

        return exercise;
    }
}

using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.UseCases.Exercises;

public class GetAllExercisesUseCase
{
    private IExerciseRepository _exercises;

    public GetAllExercisesUseCase(
        IExerciseRepository exercises
    )
    {
        _exercises = exercises;
    }

    public async Task<IEnumerable<Exercise>> Execute(CancellationToken cancellationToken)
    {
        return await _exercises.GetAll(cancellationToken);
    }
}
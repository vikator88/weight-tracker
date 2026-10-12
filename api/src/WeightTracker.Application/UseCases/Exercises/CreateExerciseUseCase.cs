using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.UseCases.Exercises;

public class CreateExerciseUseCase
{
    private IExerciseRepository _exercises;

    private IUnitOfWork _unitOfWork;

    public CreateExerciseUseCase(
        IExerciseRepository exercises,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _unitOfWork = unitOfWork;
    }

    /// <exception cref="InvalidExerciseNameException">When the name is not a valid exercise name.</exception>
    /// <exception cref="InvalidUrlException">When a video url is given and is not a valid url.</exception>
    /// <exception cref="InvalidBodyPartException">When the body part is not a defined value.</exception>
    public async Task<Exercise> Execute(ExerciseDTO dto, CancellationToken cancellationToken)
    {
        var exercise = Exercise.Create(
            ExerciseName.From(dto.Name),
            dto.VideoUrl is null ? null : Url.From(dto.VideoUrl),
            dto.BodyPart
        );

        await _exercises.Save(exercise, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return exercise;
    }
}

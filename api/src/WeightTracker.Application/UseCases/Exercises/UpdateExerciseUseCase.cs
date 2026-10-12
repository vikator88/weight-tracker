using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.UseCases.Exercises;

public class UpdateExerciseUseCase
{
    private IExerciseRepository _exercises;

    private IUnitOfWork _unitOfWork;

    public UpdateExerciseUseCase(
        IExerciseRepository exercises,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces every attribute of an existing exercise. It never creates one.
    /// </summary>
    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id.</exception>
    /// <exception cref="InvalidExerciseNameException">When the name is not a valid exercise name.</exception>
    /// <exception cref="InvalidUrlException">When a video url is given and is not a valid url.</exception>
    /// <exception cref="InvalidBodyPartException">When the body part is not a defined value.</exception>
    public async Task<Exercise> Execute(Id id, ExerciseDTO dto, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(id, cancellationToken);

        if (exercise is null)
            throw new ExerciseNotFoundException(id);

        exercise.Update(
            ExerciseName.From(dto.Name),
            dto.VideoUrl is null ? null : Url.From(dto.VideoUrl),
            dto.BodyPart
        );

        await _exercises.Save(exercise, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return exercise;
    }
}

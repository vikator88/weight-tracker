using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

public static class ExerciseMapper
{
    public static ExerciseEntity MapToEntity(Exercise exercise)
    {
        return new ExerciseEntity
        {
            Id = exercise.Id.Value,
            Name = exercise.Name,
            VideoUrl = exercise.VideoUrl,
            BodyPart = (int)exercise.BodyPart,
        };
    }

    public static Exercise MapToDomain(ExerciseEntity entity)
    {
        try
        {
            return Exercise.Rehydrate(
                Id.From(entity.Id),
                entity.Name,
                entity.VideoUrl,
                MapBodyPart(entity.BodyPart));
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored exercise '{entity.Id}' could not be restored", exception);
        }
    }

    private static BodyParts MapBodyPart(int bodyPart)
    {
        if (Enum.IsDefined(typeof(BodyParts), bodyPart) == false)
            throw new PersistenceMappingException($"'{bodyPart}' is not a known body part");

        return (BodyParts)bodyPart;
    }
}

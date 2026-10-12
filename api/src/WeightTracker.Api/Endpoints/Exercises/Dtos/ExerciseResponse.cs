using WeightTracker.Domain.Exercises;

namespace WeightTracker.Api.Endpoints.Exercises.Dtos;

public sealed record ExerciseResponse(Guid Id, string Name, string? VideoUrl, string BodyPart)
{
    public static ExerciseResponse From(Exercise exercise)
        => new(
            exercise.Id.Value,
            exercise.Name.Value,
            exercise.VideoUrl?.Value,
            exercise.BodyPart.ToString());
}

using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.UseCases.Exercises;

/// <summary>
/// Input of the create and update exercise use cases. Carries raw values; the domain
/// value objects validate them.
/// </summary>
public sealed record ExerciseDTO(string Name, string? VideoUrl, BodyParts BodyPart);

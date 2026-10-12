using System.ComponentModel.DataAnnotations;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Api.Endpoints.Exercises.Dtos;

/// <summary>
/// Body of <c>POST /exercises</c> and <c>PUT /exercises/{id}</c>. Only structural validation
/// lives here; the exercise rules are enforced by the domain.
/// </summary>
public sealed record ExerciseRequest
{
    [Required]
    public string Name { get; init; } = string.Empty;

    public string? VideoUrl { get; init; }

    [Required]
    [EnumDataType(typeof(BodyParts))]
    public BodyParts BodyPart { get; init; }

    public ExerciseDTO ToDto() => new(Name, VideoUrl, BodyPart);
}

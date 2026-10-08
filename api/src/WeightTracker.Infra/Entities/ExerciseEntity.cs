namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>exercises</c> table.</summary>
public class ExerciseEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? VideoUrl { get; set; }

    public int BodyPart { get; set; }
}

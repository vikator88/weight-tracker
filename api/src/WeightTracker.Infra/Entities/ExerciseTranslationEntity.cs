namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>ExerciseTranslations</c> table.</summary>
public class ExerciseTranslationEntity
{
    public Guid Id { get; set; }

    public Guid ExerciseId { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

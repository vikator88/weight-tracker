using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Exercises;

public sealed class Exercise : AggregateRoot
{
    /// <summary>
    /// Maximum length of the exercise name. The exercises.name column must stay aligned.
    /// </summary>
    public const int NameMaxLength = 150;

    /// <summary>
    /// Maximum length of the exercise video url. The exercises.video_url column must stay aligned.
    /// </summary>
    public const int VideoUrlMaxLength = 500;

    public string Name { get; private set; }

    public string? VideoUrl { get; set; }

    public BodyParts BodyPart { get; set; }

    public Exercise(string name, BodyParts bodyPart)
    {
        this.Id = Id.New();
        this.IsNew = true;
        Name = name; // Algo como "Peso Muerto", "Press Banca", etc...
        BodyPart = bodyPart; // LEGS, CHEST, etc...
    }

    private Exercise(Id id, string name, string? videoUrl, BodyParts bodyPart)
    {
        this.Id = id;
        this.IsNew = false;
        Name = name;
        VideoUrl = videoUrl;
        BodyPart = bodyPart;
    }

    /// <summary>
    /// Restores an exercise already stored in the system.
    /// </summary>
    public static Exercise Rehydrate(Id id, string name, string? videoUrl, BodyParts bodyPart)
        => new(id, name, videoUrl, bodyPart);
}

using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

public sealed class Exercise : AggregateRoot
{
    private BodyParts _bodyPart;

    public ExerciseName Name { get; private set; }

    public Url? VideoUrl { get; set; }

    /// <exception cref="InvalidBodyPartException">When the value is not a defined <see cref="BodyParts"/>.</exception>
    public BodyParts BodyPart
    {
        get => _bodyPart;
        set => _bodyPart = EnsureDefined(value);
    }

    private Exercise(Id id, ExerciseName name, Url? videoUrl, BodyParts bodyPart, bool isNew)
    {
        this.Id = id;
        this.IsNew = isNew;
        Name = name; // Algo como "Peso Muerto", "Press Banca", etc...
        VideoUrl = videoUrl;
        BodyPart = bodyPart; // LEGS, CHEST, etc...
    }

    /// <summary>
    /// Creates an exercise that does not exist in the system yet.
    /// </summary>
    /// <exception cref="InvalidBodyPartException">When the body part is not a defined value.</exception>
    public static Exercise Create(ExerciseName name, Url? videoUrl, BodyParts bodyPart)
        => new(Id.New(), name, videoUrl, bodyPart, isNew: true);

    /// <summary>
    /// Restores an exercise already stored in the system.
    /// </summary>
    public static Exercise Rehydrate(Id id, ExerciseName name, Url? videoUrl, BodyParts bodyPart)
        => new(id, name, videoUrl, bodyPart, isNew: false);

    /// <summary>
    /// Replaces every attribute of the exercise. A <c>null</c> video url clears the stored one.
    /// </summary>
    /// <exception cref="InvalidBodyPartException">When the body part is not a defined value.</exception>
    public void Update(ExerciseName name, Url? videoUrl, BodyParts bodyPart)
    {
        var validBodyPart = EnsureDefined(bodyPart);

        Name = name;
        VideoUrl = videoUrl;
        _bodyPart = validBodyPart;
    }

    private static BodyParts EnsureDefined(BodyParts bodyPart)
    {
        if (Enum.IsDefined(bodyPart) == false)
            throw new InvalidBodyPartException((int)bodyPart);

        return bodyPart;
    }
}

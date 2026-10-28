using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// An exercise of the catalog that workouts are built from.
/// </summary>
/// <remarks>
/// Read-only catalog: exercises are maintained in the database (migrations or direct edits) and the
/// application only restores them, so there is no factory for new exercises and no mutator.
/// </remarks>
public sealed class Exercise : AggregateRoot
{
    private readonly List<ExerciseTranslation> _translations;

    /// <summary>
    /// Entry of the free exercise dataset this exercise comes from, null when it has none.
    /// </summary>
    public FreeExerciseDbId? FreeExerciseDbId { get; }

    public Url? VideoUrl { get; }

    public Muscles Muscle { get; }

    /// <summary>
    /// Body part the muscle belongs to. Always derived from <see cref="Muscle"/>, never stored.
    /// </summary>
    public BodyParts BodyPart => MuscleBodyParts.BodyPartOf(Muscle);

    /// <summary>
    /// Kind of training the exercise belongs to. Stored, and not related to <see cref="Muscle"/> or <see cref="BodyPart"/>.
    /// </summary>
    public ExerciseTypes Type { get; }

    /// <summary>
    /// Snapshot of the names of the exercise, at most one per language.
    /// </summary>
    public IReadOnlyList<ExerciseTranslation> Translations => _translations.ToList();

    private Exercise(
        Id id,
        FreeExerciseDbId? freeExerciseDbId,
        Url? videoUrl,
        Muscles muscle,
        ExerciseTypes type,
        IEnumerable<ExerciseTranslation> translations)
    {
        var entries = translations.ToList();

        var duplicate = entries
            .GroupBy(translation => translation.LanguageCode)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new DuplicateExerciseTranslationException(duplicate.Key.Value);

        this.Id = id;
        this.IsNew = false;
        FreeExerciseDbId = freeExerciseDbId;
        VideoUrl = videoUrl;
        Muscle = EnsureDefined(muscle); // QUADRICEPS, CHEST, etc...
        Type = EnsureDefined(type);
        _translations = entries;
    }

    /// <summary>
    /// Restores an exercise stored in the catalog.
    /// </summary>
    /// <exception cref="InvalidMuscleException">When the muscle is not a defined value.</exception>
    /// <exception cref="InvalidExerciseTypeException">When the type is not a defined value.</exception>
    /// <exception cref="DuplicateExerciseTranslationException">When two translations share a language.</exception>
    public static Exercise Rehydrate(
        Id id,
        FreeExerciseDbId? freeExerciseDbId,
        Url? videoUrl,
        Muscles muscle,
        ExerciseTypes type,
        IEnumerable<ExerciseTranslation> translations)
        => new(id, freeExerciseDbId, videoUrl, muscle, type, translations);

    private static Muscles EnsureDefined(Muscles muscle)
    {
        if (Enum.IsDefined(muscle) == false)
            throw new InvalidMuscleException((int)muscle);

        return muscle;
    }

    private static ExerciseTypes EnsureDefined(ExerciseTypes type)
    {
        if (Enum.IsDefined(type) == false)
            throw new InvalidExerciseTypeException((int)type);

        return type;
    }
}

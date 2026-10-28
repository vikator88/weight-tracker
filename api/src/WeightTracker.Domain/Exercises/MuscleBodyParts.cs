using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// Maps each muscle to the body part it belongs to. The body part of an exercise is always derived from here.
/// </summary>
public static class MuscleBodyParts
{
    /// <exception cref="InvalidMuscleException">When the muscle is not a defined value.</exception>
    public static BodyParts BodyPartOf(Muscles muscle) => muscle switch
    {
        Muscles.ABDOMINALS => BodyParts.CORE,
        Muscles.ABDUCTORS => BodyParts.LEGS,
        Muscles.ADDUCTORS => BodyParts.LEGS,
        Muscles.BICEPS => BodyParts.ARMS,
        Muscles.CALVES => BodyParts.LEGS,
        Muscles.CHEST => BodyParts.CHEST,
        Muscles.FOREARMS => BodyParts.ARMS,
        Muscles.GLUTES => BodyParts.LEGS,
        Muscles.HAMSTRINGS => BodyParts.LEGS,
        Muscles.LATS => BodyParts.BACK,
        Muscles.LOWER_BACK => BodyParts.CORE,
        Muscles.MIDDLE_BACK => BodyParts.BACK,
        Muscles.NECK => BodyParts.NECK,
        Muscles.QUADRICEPS => BodyParts.LEGS,
        Muscles.SHOULDERS => BodyParts.SHOULDERS,
        Muscles.TRAPS => BodyParts.BACK,
        Muscles.TRICEPS => BodyParts.ARMS,
        _ => throw new InvalidMuscleException((int)muscle),
    };
}

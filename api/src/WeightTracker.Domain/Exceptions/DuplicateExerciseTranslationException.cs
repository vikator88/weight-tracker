namespace WeightTracker.Domain.Exceptions;

public sealed class DuplicateExerciseTranslationException : DomainException
{
    public DuplicateExerciseTranslationException(string languageCode)
        : base($"Language '{languageCode}' has more than one translation")
    { }
}

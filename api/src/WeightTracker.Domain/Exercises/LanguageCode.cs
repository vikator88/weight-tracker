using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// A lowercase ISO 639-1 two-letter language code, such as "es" or "en".
/// </summary>
/// <remarks>
/// Only the format is checked, not whether the code names a real language. Lives in
/// <c>Domain/Exercises</c> because only exercise translations use it today.
/// </remarks>
public sealed record LanguageCode
{
    /// <summary>
    /// Exact length of a code. The ExerciseTranslations.LanguageCode column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 2;

    public string Value { get; }

    private LanguageCode(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidLanguageCodeException">When the value is not two letters from a to z.</exception>
    public static LanguageCode From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidLanguageCodeException(value ?? string.Empty);

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length != MaxLength || normalized.All(character => character is >= 'a' and <= 'z') == false)
            throw new InvalidLanguageCodeException(value);

        return new LanguageCode(normalized);
    }

    public override string ToString() => Value;
}

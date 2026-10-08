using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Users;

/// <summary>
/// A person's given name or family name. The same rules govern both, so one type
/// serves <see cref="User.Name"/> and <see cref="User.Surname"/>.
/// </summary>
/// <remarks>
/// Only length is constrained. No character class is rejected, so names carrying
/// digits, punctuation or any script are accepted.
/// </remarks>
public sealed record PersonName
{
    /// <summary>
    /// Maximum length accepted. The users.name and users.surname column lengths
    /// must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 100;

    public string Value { get; }

    private PersonName(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidPersonNameException">When the value is empty or too long.</exception>
    public static PersonName From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidPersonNameException(value ?? string.Empty);

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new InvalidPersonNameException(value);

        return new PersonName(normalized);
    }

    public override string ToString() => Value;
}

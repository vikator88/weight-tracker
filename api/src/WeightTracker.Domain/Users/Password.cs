using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Users;

/// <summary>
/// A plain-text password at the moment it enters the system. This is the single
/// home of the password-strength rules.
/// </summary>
/// <remarks>
/// Transient by design: it is consumed by the hashing boundary and never assigned to
/// aggregate state, persisted, serialized or logged. <see cref="ToString"/> masks the
/// value so it cannot leak through string interpolation or a log sink.
/// The value is deliberately not trimmed, because leading and trailing whitespace is
/// significant in a password and must reach the hasher intact.
/// </remarks>
public sealed record Password
{
    /// <summary>Shortest accepted password.</summary>
    public const int MinLength = 8;

    /// <summary>
    /// Longest accepted password. Bounds the input handed to the key-derivation
    /// function so an arbitrarily long value cannot be submitted for hashing.
    /// </summary>
    public const int MaxLength = 128;

    private const string Mask = "********";

    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidPasswordException">When the value does not meet the strength requirements.</exception>
    public static Password From(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new InvalidPasswordException("it must not be empty");

        if (value.Length < MinLength)
            throw new InvalidPasswordException($"it must be at least {MinLength} characters long");

        if (value.Length > MaxLength)
            throw new InvalidPasswordException($"it must be at most {MaxLength} characters long");

        if (value.Any(char.IsUpper) == false)
            throw new InvalidPasswordException("it must contain at least one uppercase letter");

        if (value.Any(char.IsDigit) == false)
            throw new InvalidPasswordException("it must contain at least one digit");

        if (value.Any(IsSpecial) == false)
            throw new InvalidPasswordException("it must contain at least one special character");

        return new Password(value);
    }

    private static bool IsSpecial(char candidate) => char.IsLetterOrDigit(candidate) == false;

    public override string ToString() => Mask;
}

using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Users;

/// <summary>
/// An already-hashed user credential, as held by the <see cref="User"/> aggregate.
/// </summary>
/// <remarks>
/// This type describes a hash, not a plain-text password, so it carries no strength
/// rules: the stored value is a hasher output and would fail them. Strength belongs
/// to <see cref="Password"/>, at the boundary where a plain-text actually exists.
/// The value is not trimmed, because every character of a hash is significant.
/// </remarks>
public sealed record PasswordHash
{
    /// <summary>
    /// Maximum length accepted. The users.password_hash column length must stay
    /// aligned with this value.
    /// </summary>
    public const int MaxLength = 500;

    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidPasswordHashException">When the value is empty or too long.</exception>
    public static PasswordHash From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidPasswordHashException();

        if (value.Length > MaxLength)
            throw new InvalidPasswordHashException();

        return new PasswordHash(value);
    }

    public override string ToString() => Value;
}

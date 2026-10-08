using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Common;

/// <summary>
/// Email address of a system user. It is a unique business identifier, so it is
/// normalized to lowercase to make equality and uniqueness checks case-insensitive.
/// </summary>
public sealed record Email
{
    /// <summary>
    /// Maximum length accepted by RFC 5321 for a full address. The
    /// users.email column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidEmailException">When the value is not a well-formed address.</exception>
    public static Email From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidEmailException(value ?? string.Empty);

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
            throw new InvalidEmailException(value);

        if (IsWellFormed(normalized) == false)
            throw new InvalidEmailException(value);

        return new Email(normalized);
    }

    private static bool IsWellFormed(string candidate)
    {
        if (candidate.Any(char.IsWhiteSpace))
            return false;

        var parts = candidate.Split('@');

        if (parts.Length != 2)
            return false;

        var local = parts[0];
        var domain = parts[1];

        if (local.Length == 0 || domain.Length == 0)
            return false;

        if (domain.Contains('.') == false)
            return false;

        if (domain.StartsWith('.') || domain.EndsWith('.') || domain.Contains(".."))
            return false;

        return true;
    }

    public override string ToString() => Value;
}

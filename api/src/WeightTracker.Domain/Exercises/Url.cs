using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Exercises;

/// <summary>
/// An absolute http or https address whose host has at least one dot.
/// </summary>
/// <remarks>
/// Lives in <c>Domain/Exercises</c> because only exercises use it today. It moves to
/// <c>Domain/Common</c> as soon as a second domain area needs it.
/// </remarks>
public sealed record Url
{
    /// <summary>
    /// Maximum length accepted. The exercises.video_url column length must stay aligned with this value.
    /// </summary>
    public const int MaxLength = 500;

    public string Value { get; }

    private Url(string value)
    {
        Value = value;
    }

    /// <exception cref="InvalidUrlException">
    /// When the value is empty, too long, not absolute, not http or https, or its host has no dot.
    /// </exception>
    public static Url From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidUrlException(value ?? string.Empty);

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new InvalidUrlException(value);

        // Uri is forgiving (backslash schemes, embedded spaces), and the value is stored as written
        if (normalized.Any(char.IsWhiteSpace))
            throw new InvalidUrlException(value);

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) == false)
            throw new InvalidUrlException(value);

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidUrlException(value);

        if (normalized.StartsWith($"{uri.Scheme}://", StringComparison.OrdinalIgnoreCase) == false)
            throw new InvalidUrlException(value);

        if (uri.Host.Contains('.') == false)
            throw new InvalidUrlException(value);

        return new Url(normalized);
    }

    public override string ToString() => Value;
}

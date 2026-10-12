using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

public class UrlTests
{
    [Theory]
    [InlineData("http://domain.es/asdasd?x=1")]
    [InlineData("https://videos.example.com/plancha")]
    [InlineData("https://example.com")]
    public void From_ShouldAcceptAnAbsoluteHttpOrHttpsUrlWithADottedHost(string candidate)
    {
        // Arrange & Act
        var url = Url.From(candidate);

        // Assert
        url.Value.Should().Be(candidate);
    }

    [Fact]
    public void From_ShouldTrimSurroundingWhitespace()
    {
        // Arrange & Act
        var url = Url.From("  https://example.com/a  ");

        // Assert
        url.Value.Should().Be("https://example.com/a");
    }

    [Fact]
    public void From_ShouldAcceptAValueOfExactlyMaxLength()
    {
        // Arrange
        var prefix = "https://example.com/";
        var candidate = prefix + new string('a', Url.MaxLength - prefix.Length);

        // Act
        var url = Url.From(candidate);

        // Assert
        url.Value.Should().HaveLength(Url.MaxLength);
    }

    [Fact]
    public void From_ShouldRejectAValueLongerThanMaxLength()
    {
        // Arrange
        var prefix = "https://example.com/";
        var candidate = prefix + new string('a', Url.MaxLength - prefix.Length + 1);

        // Act
        var act = () => Url.From(candidate);

        // Assert
        act.Should().Throw<InvalidUrlException>();
    }

    [Theory]
    [InlineData("/videos/plancha")]
    [InlineData("example.com/plancha")]
    [InlineData("not a url")]
    public void From_ShouldRejectAValueThatIsNotAbsolute(string candidate)
    {
        // Arrange & Act
        var act = () => Url.From(candidate);

        // Assert
        act.Should().Throw<InvalidUrlException>();
    }

    [Theory]
    [InlineData("ftp://example.com/file")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    public void From_ShouldRejectASchemeOtherThanHttpOrHttps(string candidate)
    {
        // Arrange & Act
        var act = () => Url.From(candidate);

        // Assert
        act.Should().Throw<InvalidUrlException>();
    }

    [Theory]
    [InlineData("http:\\\\example.com")]
    [InlineData("https:/example.com")]
    [InlineData("https:example.com")]
    public void From_ShouldRejectAValueWithoutTheSchemeSeparator(string candidate)
    {
        // Arrange & Act
        var act = () => Url.From(candidate);

        // Assert
        act.Should().Throw<InvalidUrlException>();
    }

    [Theory]
    [InlineData("https://example.com/a b")]
    [InlineData("https://example.com/a\tb")]
    public void From_ShouldRejectAValueContainingWhitespace(string candidate)
    {
        // Arrange & Act
        var act = () => Url.From(candidate);

        // Assert
        act.Should().Throw<InvalidUrlException>();
    }

    [Fact]
    public void From_ShouldAcceptAnUppercaseScheme()
    {
        // Arrange & Act
        var url = Url.From("HTTPS://Example.com/a");

        // Assert
        url.Value.Should().Be("HTTPS://Example.com/a");
    }

    [Theory]
    [InlineData("https://video")]
    [InlineData("http://localhost/a")]
    public void From_ShouldRejectAHostWithoutADot(string candidate)
    {
        // Arrange & Act
        var act = () => Url.From(candidate);

        // Assert
        act.Should().Throw<InvalidUrlException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldRejectANullEmptyOrWhitespaceValue(string? candidate)
    {
        // Arrange & Act
        var act = () => Url.From(candidate);

        // Assert
        act.Should().Throw<InvalidUrlException>();
    }
}

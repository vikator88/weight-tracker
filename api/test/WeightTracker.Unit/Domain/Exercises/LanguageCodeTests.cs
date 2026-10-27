using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

public class LanguageCodeTests
{
    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    public void From_ShouldAcceptATwoLetterLowercaseCode(string candidate)
    {
        // Arrange & Act
        var code = LanguageCode.From(candidate);

        // Assert
        code.Value.Should().Be(candidate);
    }

    [Fact]
    public void From_ShouldTrimAndLowercaseTheValue()
    {
        // Arrange & Act
        var code = LanguageCode.From(" ES ");

        // Assert
        code.Value.Should().Be("es");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("e")]
    [InlineData("spa")]
    [InlineData("e1")]
    [InlineData("e-")]
    [InlineData("ñe")]
    public void From_ShouldRejectAnInvalidValue(string? candidate)
    {
        // Arrange & Act
        var act = () => LanguageCode.From(candidate);

        // Assert
        act.Should().Throw<InvalidLanguageCodeException>();
    }

    [Fact]
    public void Equality_ShouldHoldForCodesThatNormalizeToTheSameValue()
    {
        // Arrange
        var lower = LanguageCode.From("es");

        // Act
        var upper = LanguageCode.From("ES");

        // Assert
        upper.Should().Be(lower);
    }
}

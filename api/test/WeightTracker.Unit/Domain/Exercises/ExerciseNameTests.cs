using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

public class ExerciseNameTests
{
    [Theory]
    [InlineData("Peso Muerto")]
    [InlineData("Press Banca")]
    [InlineData("Dip")]
    public void From_ShouldAcceptAValueWithinTheLengthLimits(string candidate)
    {
        // Arrange & Act
        var name = ExerciseName.From(candidate);

        // Assert
        name.Value.Should().Be(candidate);
    }

    [Fact]
    public void From_ShouldTrimSurroundingWhitespace()
    {
        // Arrange & Act
        var name = ExerciseName.From("  Press Banca  ");

        // Assert
        name.Value.Should().Be("Press Banca");
    }

    [Fact]
    public void From_ShouldAcceptAValueOfExactlyMaxLength()
    {
        // Arrange
        var candidate = new string('a', ExerciseName.MaxLength);

        // Act
        var name = ExerciseName.From(candidate);

        // Assert
        name.Value.Should().HaveLength(ExerciseName.MaxLength);
    }

    [Fact]
    public void From_ShouldAcceptAValueOfExactlyMinLengthAfterTrimming()
    {
        // Arrange & Act
        var name = ExerciseName.From("  abc  ");

        // Assert
        name.Value.Should().Be("abc");
    }

    [Fact]
    public void From_ShouldRejectAValueLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', ExerciseName.MaxLength + 1);

        // Act
        var act = () => ExerciseName.From(candidate);

        // Assert
        act.Should().Throw<InvalidExerciseNameException>();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("  ab  ")]
    public void From_ShouldRejectAValueShorterThanMinLength(string candidate)
    {
        // Arrange & Act
        var act = () => ExerciseName.From(candidate);

        // Assert
        act.Should().Throw<InvalidExerciseNameException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldRejectANullEmptyOrWhitespaceValue(string? candidate)
    {
        // Arrange & Act
        var act = () => ExerciseName.From(candidate);

        // Assert
        act.Should().Throw<InvalidExerciseNameException>();
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        // Arrange
        var first = ExerciseName.From("Press Banca");

        // Act
        var second = ExerciseName.From("Press Banca");

        // Assert
        first.Should().Be(second);
    }
}

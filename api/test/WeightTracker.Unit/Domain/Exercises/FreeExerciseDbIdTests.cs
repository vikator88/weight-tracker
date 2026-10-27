using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

public class FreeExerciseDbIdTests
{
    [Theory]
    [InlineData("Barbell_Squat")]
    [InlineData("3_4_Sit-Up")]
    [InlineData("Barbell_Bench_Press_-_Medium_Grip")]
    public void From_ShouldAcceptADatasetId(string candidate)
    {
        // Arrange & Act
        var id = FreeExerciseDbId.From(candidate);

        // Assert
        id.Value.Should().Be(candidate);
    }

    [Fact]
    public void From_ShouldTrimSurroundingWhitespace()
    {
        // Arrange & Act
        var id = FreeExerciseDbId.From("  Barbell_Curl_Lying  ");

        // Assert
        id.Value.Should().Be("Barbell_Curl_Lying");
    }

    [Fact]
    public void From_ShouldAcceptAValueOfExactlyMaxLength()
    {
        // Arrange
        var candidate = new string('a', FreeExerciseDbId.MaxLength);

        // Act
        var id = FreeExerciseDbId.From(candidate);

        // Assert
        id.Value.Should().HaveLength(FreeExerciseDbId.MaxLength);
    }

    [Fact]
    public void From_ShouldRejectAValueLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', FreeExerciseDbId.MaxLength + 1);

        // Act
        var act = () => FreeExerciseDbId.From(candidate);

        // Assert
        act.Should().Throw<InvalidFreeExerciseDbIdException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldRejectAnEmptyValue(string? candidate)
    {
        // Arrange & Act
        var act = () => FreeExerciseDbId.From(candidate);

        // Assert
        act.Should().Throw<InvalidFreeExerciseDbIdException>();
    }
}

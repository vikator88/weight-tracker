using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WorkoutDescriptionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldReturnNullForBlankValues(string? candidate)
    {
        // Arrange & Act
        var description = WorkoutDescription.From(candidate);

        // Assert
        description.Should().BeNull();
    }

    [Fact]
    public void From_ShouldTrimTheValue()
    {
        // Arrange & Act
        var description = WorkoutDescription.From("  Focus on tempo  ");

        // Assert
        description!.Value.Should().Be("Focus on tempo");
    }

    [Fact]
    public void From_ShouldAcceptMaxLength()
    {
        // Arrange
        var candidate = new string('a', WorkoutDescription.MaxLength);

        // Act
        var description = WorkoutDescription.From(candidate);

        // Assert
        description!.Value.Should().HaveLength(1024);
    }

    [Fact]
    public void From_ShouldRejectValuesLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', WorkoutDescription.MaxLength + 1);

        // Act
        var act = () => WorkoutDescription.From(candidate);

        // Assert
        act.Should().Throw<InvalidWorkoutDescriptionException>();
    }
}

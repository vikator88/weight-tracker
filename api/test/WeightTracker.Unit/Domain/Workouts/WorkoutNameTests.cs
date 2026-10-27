using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WorkoutNameTests
{
    [Fact]
    public void From_ShouldTrimTheValue()
    {
        // Arrange & Act
        var name = WorkoutName.From("  Leg day  ");

        // Assert
        name.Value.Should().Be("Leg day");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldRejectBlankValues(string? candidate)
    {
        // Arrange & Act
        var act = () => WorkoutName.From(candidate);

        // Assert
        act.Should().Throw<InvalidWorkoutNameException>();
    }

    [Fact]
    public void From_ShouldAcceptMaxLength()
    {
        // Arrange
        var candidate = new string('a', WorkoutName.MaxLength);

        // Act
        var name = WorkoutName.From(candidate);

        // Assert
        name.Value.Should().HaveLength(256);
    }

    [Fact]
    public void From_ShouldRejectValuesLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', WorkoutName.MaxLength + 1);

        // Act
        var act = () => WorkoutName.From(candidate);

        // Assert
        act.Should().Throw<InvalidWorkoutNameException>();
    }

    [Fact]
    public void From_ShouldMeasureLengthAfterTrimming()
    {
        // Arrange
        var candidate = $"  {new string('a', WorkoutName.MaxLength)}  ";

        // Act
        var name = WorkoutName.From(candidate);

        // Assert
        name.Value.Should().HaveLength(256);
    }
}

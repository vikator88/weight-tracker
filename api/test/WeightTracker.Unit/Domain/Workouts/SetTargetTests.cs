using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class SetTargetTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Reps_ShouldRejectAValueBelowOne(int value)
    {
        // Arrange & Act
        var act = () => new SetTarget.Reps(value);

        // Assert
        act.Should().Throw<InvalidSetPrescriptionException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Duration_ShouldRejectAValueBelowOne(int value)
    {
        // Arrange & Act
        var act = () => new SetTarget.Duration(value);

        // Assert
        act.Should().Throw<InvalidSetPrescriptionException>();
    }

    [Fact]
    public void Reps_ShouldAcceptAValueOfOne()
    {
        // Arrange & Act
        var target = new SetTarget.Reps(1);

        // Assert
        target.value.Should().Be(1);
    }

    [Fact]
    public void Duration_ShouldKeepTheSeconds()
    {
        // Arrange & Act
        var target = new SetTarget.Duration(40);

        // Assert
        target.value.Should().Be(40);
    }

    [Fact]
    public void MaxReps_ShouldCarryNoValue()
    {
        // Arrange & Act
        var target = new SetTarget.MaxReps();

        // Assert
        target.Should().Be(new SetTarget.MaxReps());
    }
}

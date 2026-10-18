using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class SetPrescriptionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldRejectACountBelowOne(int count)
    {
        // Arrange
        var target = new SetTarget.Reps(10);

        // Act
        var act = () => new SetPrescription(count, target);

        // Assert
        act.Should().Throw<InvalidSetPrescriptionException>();
    }

    [Fact]
    public void Constructor_ShouldAcceptACountOfOne()
    {
        // Arrange & Act
        var prescription = new SetPrescription(1, new SetTarget.MaxReps());

        // Assert
        prescription.count.Should().Be(1);
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        // Arrange
        var first = new SetPrescription(3, new SetTarget.Reps(10));

        // Act
        var second = new SetPrescription(3, new SetTarget.Reps(10));

        // Assert
        second.Should().Be(first);
    }
}

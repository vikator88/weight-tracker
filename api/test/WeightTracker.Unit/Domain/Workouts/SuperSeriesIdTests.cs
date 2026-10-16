using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class SuperSeriesIdTests
{
    [Fact]
    public void From_ShouldKeepAPositiveValue()
    {
        // Arrange & Act
        var id = SuperSeriesId.From(3);

        // Assert
        id.Value.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void From_ShouldRejectAValueBelowOne(int value)
    {
        // Arrange & Act
        var act = () => SuperSeriesId.From(value);

        // Assert
        act.Should().Throw<InvalidSuperSeriesIdException>();
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        // Arrange
        var first = SuperSeriesId.From(2);

        // Act
        var second = SuperSeriesId.From(2);

        // Assert
        second.Should().Be(first);
    }
}

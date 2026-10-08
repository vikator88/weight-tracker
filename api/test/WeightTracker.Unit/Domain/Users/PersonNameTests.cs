using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Domain.Users;

public class PersonNameTests
{
    [Theory]
    [InlineData("Ada")]
    [InlineData("Lovelace")]
    [InlineData("Grace Brewster Murray")]
    [InlineData("O'Brien")]
    [InlineData("Ada-Maria")]
    [InlineData("Ada 2nd")]
    public void From_ShouldAcceptAnyValueWithinTheLengthLimit(string candidate)
    {
        // Arrange & Act
        var name = PersonName.From(candidate);

        // Assert
        name.Value.Should().Be(candidate);
    }

    [Fact]
    public void From_ShouldAcceptAValueOfExactlyMaxLength()
    {
        // Arrange
        var candidate = new string('a', PersonName.MaxLength);

        // Act
        var name = PersonName.From(candidate);

        // Assert
        name.Value.Should().HaveLength(PersonName.MaxLength);
    }

    [Fact]
    public void From_ShouldRejectAValueLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', PersonName.MaxLength + 1);

        // Act
        var act = () => PersonName.From(candidate);

        // Assert
        act.Should().Throw<InvalidPersonNameException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_ShouldRejectAnEmptyValue(string? candidate)
    {
        // Arrange & Act
        var act = () => PersonName.From(candidate);

        // Assert
        act.Should().Throw<InvalidPersonNameException>();
    }

    [Fact]
    public void From_ShouldTrimSurroundingWhitespace()
    {
        // Arrange & Act
        var name = PersonName.From("  Ada  ");

        // Assert
        name.Value.Should().Be("Ada");
    }

    [Fact]
    public void From_ShouldMeasureLengthAfterTrimming()
    {
        // Arrange
        var candidate = $"  {new string('a', PersonName.MaxLength)}  ";

        // Act
        var name = PersonName.From(candidate);

        // Assert
        name.Value.Should().HaveLength(PersonName.MaxLength);
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        // Arrange & Act
        var first = PersonName.From("Ada");
        var second = PersonName.From("Ada");

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void Equality_ShouldBeCaseSensitive()
    {
        // Arrange & Act
        var capitalized = PersonName.From("Ada");
        var lowercase = PersonName.From("ada");

        // Assert
        capitalized.Should().NotBe(lowercase);
    }

    [Fact]
    public void DifferentNames_ShouldNotBeEqual()
    {
        // Arrange & Act
        var name = PersonName.From("Ada");
        var surname = PersonName.From("Lovelace");

        // Assert
        name.Should().NotBe(surname);
    }

    [Fact]
    public void ToString_ShouldReturnTheValue()
    {
        // Arrange & Act
        var name = PersonName.From("Lovelace");

        // Assert
        name.ToString().Should().Be("Lovelace");
    }
}

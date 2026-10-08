using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using Xunit;

namespace WeightTracker.Unit.Domain.Common;

public class IdTests
{
    [Fact]
    public void New_ShouldGenerateNonEmptyIdentifier()
    {
        // Arrange & Act
        var id = Id.New();

        // Assert
        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void New_ShouldGenerateVersion7Guid()
    {
        // Arrange & Act
        var id = Id.New();

        // Assert
        id.Value.Version.Should().Be(7);
    }

    [Fact]
    public void New_ShouldGenerateDifferentIdentifiersOnEachCall()
    {
        // Arrange
        var first = Id.New();

        // Act
        var second = Id.New();

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void From_ShouldRejectEmptyGuid()
    {
        // Arrange & Act
        var act = () => Id.From(Guid.Empty);

        // Assert
        act.Should().Throw<InvalidIdException>();
    }

    [Fact]
    public void From_ShouldRejectMalformedText()
    {
        // Arrange & Act
        var act = () => Id.From("not-an-identifier");

        // Assert
        act.Should().Throw<InvalidIdException>();
    }

    [Fact]
    public void From_ShouldKeepTheProvidedValue()
    {
        // Arrange
        var value = Guid.CreateVersion7();

        // Act
        var id = Id.From(value);

        // Assert
        id.Value.Should().Be(value);
    }

    [Fact]
    public void Identifiers_WithSameValue_ShouldBeEqual()
    {
        // Arrange & Act
        var value = Guid.CreateVersion7();

        // Assert
        Id.From(value).Should().Be(Id.From(value));
    }

    [Fact]
    public void Identifiers_WithSameValue_ShouldShareHashCode()
    {
        // Arrange & Act
        var value = Guid.CreateVersion7();

        // Assert
        Id.From(value).GetHashCode().Should().Be(Id.From(value).GetHashCode());
    }

    [Fact]
    public void TryFrom_ShouldFailOnEmptyGuidText()
    {
        // Arrange & Act
        var parsed = Id.TryFrom(Guid.Empty.ToString(), out var id);

        // Assert
        parsed.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public void TryFrom_ShouldSucceedOnValidText()
    {
        // Arrange
        var value = Guid.CreateVersion7();

        // Act
        var parsed = Id.TryFrom(value.ToString(), out var id);

        // Assert
        parsed.Should().BeTrue();
        id!.Value.Should().Be(value);
    }

    [Fact]
    public void ToString_ShouldReturnTheUnderlyingValue()
    {
        // Arrange & Act
        var value = Guid.CreateVersion7();

        // Assert
        Id.From(value).ToString().Should().Be(value.ToString());
    }
}

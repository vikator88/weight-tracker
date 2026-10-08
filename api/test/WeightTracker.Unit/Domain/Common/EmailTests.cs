using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using Xunit;

namespace WeightTracker.Unit.Domain.Common;

public class EmailTests
{
    [Theory]
    [InlineData("user@weighttracker.test")]
    [InlineData("first.last@sub.domain.org")]
    [InlineData("trainer+tag@weighttracker.test")]
    public void From_ShouldAcceptWellFormedAddresses(string candidate)
    {
        // Arrange & Act
        var email = Email.From(candidate);

        // Assert
        email.Value.Should().Be(candidate.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("no-at-sign.test")]
    [InlineData("@weighttracker.test")]
    [InlineData("user@")]
    [InlineData("user@localhost")]
    [InlineData("user@.weighttracker.test")]
    [InlineData("user@weighttracker.test.")]
    [InlineData("user@weighttracker..test")]
    [InlineData("two@at@weighttracker.test")]
    [InlineData("white space@weighttracker.test")]
    public void From_ShouldRejectMalformedAddresses(string? candidate)
    {
        // Arrange & Act
        var act = () => Email.From(candidate);

        // Assert
        act.Should().Throw<InvalidEmailException>();
    }

    [Fact]
    public void From_ShouldRejectAddressesLongerThanMaxLength()
    {
        // Arrange
        var local = new string('a', Email.MaxLength);

        // Act
        var act = () => Email.From($"{local}@weighttracker.test");

        // Assert
        act.Should().Throw<InvalidEmailException>();
    }

    [Fact]
    public void From_ShouldNormalizeToLowercase()
    {
        // Arrange & Act
        var email = Email.From("User.Name@WeightTracker.TEST");

        // Assert
        email.Value.Should().Be("user.name@weighttracker.test");
    }

    [Fact]
    public void From_ShouldTrimSurroundingWhitespace()
    {
        // Arrange & Act
        var email = Email.From("  user@weighttracker.test  ");

        // Assert
        email.Value.Should().Be("user@weighttracker.test");
    }

    [Fact]
    public void Equality_ShouldBeCaseInsensitive()
    {
        // Arrange & Act
        var upperCase = Email.From("USER@weighttracker.test");
        var lowerCase = Email.From("user@weighttracker.test");

        // Assert
        upperCase.Should().Be(lowerCase);
    }

    [Fact]
    public void Equality_ShouldShareHashCodeAcrossCasing()
    {
        // Arrange & Act
        var upperCase = Email.From("USER@weighttracker.test");
        var lowerCase = Email.From("user@weighttracker.test");

        // Assert
        upperCase.GetHashCode().Should().Be(lowerCase.GetHashCode());
    }

    [Fact]
    public void DifferentAddresses_ShouldNotBeEqual()
    {
        // Arrange & Act
        var user = Email.From("user@weighttracker.test");
        var trainer = Email.From("trainer@weighttracker.test");

        // Assert
        user.Should().NotBe(trainer);
    }
}

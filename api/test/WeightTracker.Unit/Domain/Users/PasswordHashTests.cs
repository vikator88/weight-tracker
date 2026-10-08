using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Domain.Users;

public class PasswordHashTests
{
    /// <summary>A representative PBKDF2 output, in the shape the ASP.NET Core hasher produces.</summary>
    private const string RealHash =
        "AQAAAAIAAYagAAAAEJ8kPbGZ1bQ0QF9dXx2mN3bK4rT6sV8wY0zA1cB2dE3fG4hI5jK6lM7nO8pQ9rS0tU==";

    [Fact]
    public void From_ShouldAcceptARealHash()
    {
        // Arrange & Act
        var hash = PasswordHash.From(RealHash);

        // Assert
        hash.Value.Should().Be(RealHash);
    }

    [Fact]
    public void From_ShouldAcceptAValueThatWouldFailThePasswordStrengthRules()
    {
        // Arrange & Act
        var hash = PasswordHash.From("hash");

        // Assert
        hash.Value.Should().Be("hash", "a stored hash is not a plain-text password and carries no strength rules");
    }

    [Fact]
    public void From_ShouldAcceptAValueOfExactlyMaxLength()
    {
        // Arrange
        var candidate = new string('a', PasswordHash.MaxLength);

        // Act
        var hash = PasswordHash.From(candidate);

        // Assert
        hash.Value.Should().HaveLength(PasswordHash.MaxLength);
    }

    [Fact]
    public void From_ShouldRejectAValueLongerThanMaxLength()
    {
        // Arrange
        var candidate = new string('a', PasswordHash.MaxLength + 1);

        // Act
        var act = () => PasswordHash.From(candidate);

        // Assert
        act.Should().Throw<InvalidPasswordHashException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_ShouldRejectAnEmptyValue(string? candidate)
    {
        // Arrange & Act
        var act = () => PasswordHash.From(candidate);

        // Assert
        act.Should().Throw<InvalidPasswordHashException>();
    }

    [Fact]
    public void From_ShouldNotTrimTheValue()
    {
        // Arrange & Act
        var hash = PasswordHash.From("  hash  ");

        // Assert
        hash.Value.Should().Be("  hash  ", "every character of a stored hash is significant");
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        // Arrange & Act
        var first = PasswordHash.From(RealHash);
        var second = PasswordHash.From(RealHash);

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void DifferentHashes_ShouldNotBeEqual()
    {
        // Arrange & Act
        var first = PasswordHash.From(RealHash);
        var second = PasswordHash.From("another-hash");

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void ToString_ShouldReturnTheValue()
    {
        // Arrange & Act
        var hash = PasswordHash.From(RealHash);

        // Assert
        hash.ToString().Should().Be(RealHash);
    }
}

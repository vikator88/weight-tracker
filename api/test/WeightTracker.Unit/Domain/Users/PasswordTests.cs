using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Domain.Users;

public class PasswordTests
{
    [Theory]
    [InlineData("Passw0rd!")]
    [InlineData("Str0ng#Secret")]
    [InlineData("A1b2C3d4$")]
    [InlineData("Tr4iner@2026")]
    public void From_ShouldAcceptAStrongPassword(string candidate)
    {
        // Arrange & Act
        var password = Password.From(candidate);

        // Assert
        password.Value.Should().Be(candidate);
    }

    [Fact]
    public void From_ShouldAcceptTheSeededCredential()
    {
        // Arrange & Act
        var password = Password.From("Passw0rd!");

        // Assert
        password.Value.Should().Be(
            "Passw0rd!",
            "the seeded development and test credential must satisfy the strength rules or seeding breaks");
    }

    [Fact]
    public void From_ShouldAcceptAPasswordOfExactlyMinLength()
    {
        // Arrange
        var candidate = "Pa55wd!x";

        // Act
        var password = Password.From(candidate);

        // Assert
        password.Value.Should().HaveLength(Password.MinLength);
    }

    [Fact]
    public void From_ShouldRejectAPasswordShorterThanMinLength()
    {
        // Arrange
        var candidate = "Pa55wd!";

        // Act
        var act = () => Password.From(candidate);

        // Assert
        act.Should().Throw<InvalidPasswordException>();
    }

    [Fact]
    public void From_ShouldAcceptAPasswordOfExactlyMaxLength()
    {
        // Arrange
        var candidate = $"A1!{new string('a', Password.MaxLength - 3)}";

        // Act
        var password = Password.From(candidate);

        // Assert
        password.Value.Should().HaveLength(Password.MaxLength);
    }

    [Fact]
    public void From_ShouldRejectAPasswordLongerThanMaxLength()
    {
        // Arrange
        var candidate = $"A1!{new string('a', Password.MaxLength - 2)}";

        // Act
        var act = () => Password.From(candidate);

        // Assert
        act.Should().Throw<InvalidPasswordException>();
    }

    [Fact]
    public void From_ShouldRejectAPasswordWithoutAnUppercaseLetter()
    {
        // Arrange & Act
        var act = () => Password.From("passw0rd!");

        // Assert
        act.Should().Throw<InvalidPasswordException>();
    }

    [Fact]
    public void From_ShouldRejectAPasswordWithoutADigit()
    {
        // Arrange & Act
        var act = () => Password.From("Password!");

        // Assert
        act.Should().Throw<InvalidPasswordException>();
    }

    [Fact]
    public void From_ShouldRejectAPasswordWithoutASpecialCharacter()
    {
        // Arrange & Act
        var act = () => Password.From("Passw0rdd");

        // Assert
        act.Should().Throw<InvalidPasswordException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void From_ShouldRejectAnEmptyValue(string? candidate)
    {
        // Arrange & Act
        var act = () => Password.From(candidate);

        // Assert
        act.Should().Throw<InvalidPasswordException>();
    }

    [Fact]
    public void From_ShouldPreserveSurroundingWhitespace()
    {
        // Arrange & Act
        var password = Password.From(" Passw0rd! ");

        // Assert
        password.Value.Should().Be(
            " Passw0rd! ", "whitespace is significant in a password and must reach the hasher intact");
    }

    [Fact]
    public void From_ShouldCountWhitespaceAsASpecialCharacter()
    {
        // Arrange & Act
        var password = Password.From("Passw0rd d");

        // Assert
        password.Value.Should().Be("Passw0rd d");
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        // Arrange & Act
        var first = Password.From("Passw0rd!");
        var second = Password.From("Passw0rd!");

        // Assert
        first.Should().Be(second);
    }

    [Fact]
    public void ToString_ShouldNotRevealThePlainText()
    {
        // Arrange & Act
        var password = Password.From("Passw0rd!");

        // Assert
        password.ToString().Should().NotContain("Passw0rd");
    }

    [Fact]
    public void ToString_ShouldReturnAMask()
    {
        // Arrange & Act
        var password = Password.From("Passw0rd!");

        // Assert
        password.ToString().Should().Be("********");
    }

    [Fact]
    public void Interpolation_ShouldNotRevealThePlainText()
    {
        // Arrange
        var password = Password.From("Passw0rd!");

        // Act
        var interpolated = $"{password}";

        // Assert
        interpolated.Should().NotContain("Passw0rd");
    }
}

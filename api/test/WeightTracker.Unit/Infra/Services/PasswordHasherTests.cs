using FluentAssertions;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Services;
using Xunit;

namespace WeightTracker.Unit.Infra.Services;

/// <summary>
/// Hashing round trip and corrupt-storage handling for the PBKDF2 hasher.
/// </summary>
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldNotReturnThePlainText()
    {
        // Arrange
        var password = Password.From("Passw0rd!");

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Value.Should().NotBe("Passw0rd!", "credentials are stored hashed, never in plain text");
    }

    [Fact]
    public void Hash_ShouldProduceAValueWithinThePasswordHashLimit()
    {
        // Arrange
        var password = Password.From("Passw0rd!");

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Value.Length.Should().BeLessThanOrEqualTo(PasswordHash.MaxLength);
    }

    [Fact]
    public void Hash_ShouldBeSaltedSoTheSamePasswordHashesDifferently()
    {
        // Arrange
        var password = Password.From("Passw0rd!");

        // Act
        var first = _hasher.Hash(password);

        // Assert
        first.Should().NotBe(_hasher.Hash(password));
    }

    [Fact]
    public void Verify_ShouldAcceptTheOriginalPassword()
    {
        // Arrange
        var hash = _hasher.Hash(Password.From("Passw0rd!"));

        // Act
        var verified = _hasher.Verify("Passw0rd!", hash);

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldRejectADifferentPassword()
    {
        // Arrange
        var hash = _hasher.Hash(Password.From("Passw0rd!"));

        // Act
        var verified = _hasher.Verify("Different1!", hash);

        // Assert
        verified.Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldRejectAnEmptySubmittedPassword()
    {
        // Arrange
        var hash = _hasher.Hash(Password.From("Passw0rd!"));

        // Act
        var verified = _hasher.Verify(string.Empty, hash);

        // Assert
        verified.Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldFailRatherThanThrowOnACorruptStoredHash()
    {
        // Arrange
        var corrupt = PasswordHash.From("not-a-real-hash");

        // Act
        var verified = _hasher.Verify("Passw0rd!", corrupt);

        // Assert
        verified.Should().BeFalse("a corrupt stored hash must fail verification, not crash the request");
    }
}

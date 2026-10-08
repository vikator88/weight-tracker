using FluentAssertions;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using Xunit;

namespace WeightTracker.Unit.Domain.Auth;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private static RefreshToken ANewToken(DateTime? expiresAt = null)
        => RefreshToken.Create(Id.New(), "token-hash", expiresAt ?? Now.AddDays(30), Now);

    [Fact]
    public void Create_ShouldMarkTheTokenAsNew()
    {
        // Arrange & Act
        var token = ANewToken();

        // Assert
        token.IsNew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldLeaveTheTokenUnrevoked()
    {
        // Arrange & Act
        var token = ANewToken();

        // Assert
        token.RevokedAt.Should().BeNull();
        token.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public void IsActive_ShouldBeTrueWhenNotExpiredAndNotRevoked()
    {
        // Arrange
        var token = ANewToken();

        // Act
        var isActive = token.IsActive(Now);

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ShouldBeFalseWhenExpired()
    {
        // Arrange
        var token = ANewToken(expiresAt: Now.AddMinutes(-1));

        // Act
        var isActive = token.IsActive(Now);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldBeFalseExactlyAtExpiry()
    {
        // Arrange
        var token = ANewToken(expiresAt: Now);

        // Act
        var isActive = token.IsActive(Now);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_ShouldBeFalseWhenRevoked()
    {
        // Arrange
        var token = ANewToken();

        // Act
        token.Revoke(Id.New(), Now);

        // Assert
        token.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Revoke_ShouldRecordTimestampAndReplacement()
    {
        // Arrange
        var token = ANewToken();
        var replacement = Id.New();

        // Act
        token.Revoke(replacement, Now);

        // Assert
        token.RevokedAt.Should().Be(Now);
        token.ReplacedByTokenId.Should().Be(replacement);
    }

    [Fact]
    public void Revoke_ShouldRejectAnAlreadyRevokedToken()
    {
        // Arrange
        var token = ANewToken();
        token.Revoke(Id.New(), Now);

        // Act
        var act = () => token.Revoke(Id.New(), Now.AddMinutes(1));

        // Assert
        act.Should().Throw<RefreshTokenAlreadyRevokedException>();
    }

    [Fact]
    public void Revoke_ShouldNotOverwriteTheOriginalRevocationOnReplay()
    {
        // Arrange
        var token = ANewToken();
        var firstReplacement = Id.New();
        token.Revoke(firstReplacement, Now);

        // Act
        var act = () => token.Revoke(Id.New(), Now.AddMinutes(1));

        // Assert
        act.Should().Throw<RefreshTokenAlreadyRevokedException>();

        token.RevokedAt.Should().Be(Now);
        token.ReplacedByTokenId.Should().Be(firstReplacement);
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheTokenAsNew()
    {
        // Arrange & Act
        var token = RefreshToken.Rehydrate(
            Id.New(), Id.New(), "token-hash", Now.AddDays(30), null, null, Now);

        // Assert
        token.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreRevocationState()
    {
        // Arrange
        var replacement = Id.New();

        // Act
        var token = RefreshToken.Rehydrate(
            Id.New(), Id.New(), "token-hash", Now.AddDays(30), Now, replacement, Now.AddDays(-1));

        // Assert
        token.RevokedAt.Should().Be(Now);
        token.ReplacedByTokenId.Should().Be(replacement);
        token.IsActive(Now).Should().BeFalse();
    }
}

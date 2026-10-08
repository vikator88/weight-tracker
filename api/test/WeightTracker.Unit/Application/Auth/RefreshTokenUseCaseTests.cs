using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Auth;

public class RefreshTokenUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly User _user = User.Rehydrate(
        Id.New(), Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.USER, PasswordHash.From("stored-hash"), Now);

    public RefreshTokenUseCaseTests()
    {
        _clock.SetupGet(clock => clock.UtcNow).Returns(Now);
        _tokens.Setup(service => service.HashRefreshToken("raw-refresh")).Returns("hashed-refresh");
        _tokens.Setup(service => service.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new GeneratedAccessToken("new-access-token", 900));
        _tokens.Setup(service => service.GenerateRefreshToken())
            .Returns(new GeneratedRefreshToken("new-raw-refresh", "new-hashed-refresh", Now.AddDays(30)));
        _users.Setup(repository => repository.GetById(It.IsAny<Id>(), It.IsAny<CancellationToken>())).ReturnsAsync(_user);
    }

    private RefreshTokenUseCase AUseCase() => new(
        _users.Object, _refreshTokens.Object, _tokens.Object, _clock.Object, _unitOfWork.Object);

    private RefreshToken AStoredToken(DateTime? expiresAt = null)
        => RefreshToken.Create(_user.Id, "hashed-refresh", expiresAt ?? Now.AddDays(30), Now.AddDays(-1));

    [Fact]
    public async Task EmptyToken_ShouldBeRejected()
    {
        // Arrange & Act
        var act = async () => await AUseCase().Execute("   ", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task UnknownToken_ShouldBeRejected()
    {
        // Arrange
        _refreshTokens.Setup(repository => repository.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = async () => await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task ExpiredToken_ShouldBeRejected()
    {
        // Arrange
        _refreshTokens.Setup(repository => repository.GetByTokenHash("hashed-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AStoredToken(expiresAt: Now.AddMinutes(-1)));

        // Act
        var act = async () => await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RevokedToken_ShouldBeRejected()
    {
        // Arrange
        var revoked = AStoredToken();
        revoked.Revoke(Id.New(), Now.AddHours(-1));
        _refreshTokens.Setup(repository => repository.GetByTokenHash("hashed-refresh", It.IsAny<CancellationToken>())).ReturnsAsync(revoked);

        // Act
        var act = async () => await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task TokenOfAMissingUser_ShouldBeRejected()
    {
        // Arrange
        _refreshTokens.Setup(repository => repository.GetByTokenHash("hashed-refresh", It.IsAny<CancellationToken>())).ReturnsAsync(AStoredToken());
        _users.Setup(repository => repository.GetById(It.IsAny<Id>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = async () => await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task ValidToken_ShouldReturnANewPair()
    {
        // Arrange
        _refreshTokens.Setup(repository => repository.GetByTokenHash("hashed-refresh", It.IsAny<CancellationToken>())).ReturnsAsync(AStoredToken());

        // Act
        var result = await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-raw-refresh");
        result.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task ValidToken_ShouldRevokeThePresentedTokenAndLinkTheReplacement()
    {
        // Arrange
        var stored = AStoredToken();
        _refreshTokens.Setup(repository => repository.GetByTokenHash("hashed-refresh", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var saved = new List<RefreshToken>();
        _refreshTokens.Setup(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => saved.Add(token))
            .Returns(Task.CompletedTask);

        // Act
        await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        stored.RevokedAt.Should().Be(Now);
        stored.IsActive(Now).Should().BeFalse();

        var replacement = saved.Single(token => token.TokenHash == "new-hashed-refresh");

        stored.ReplacedByTokenId.Should().Be(replacement.Id);
        replacement.UserId.Should().Be(_user.Id);
        replacement.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public async Task ValidToken_ShouldPersistBothTokensAndCommitOnce()
    {
        // Arrange
        _refreshTokens.Setup(repository => repository.GetByTokenHash("hashed-refresh", It.IsAny<CancellationToken>())).ReturnsAsync(AStoredToken());

        // Act
        await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        _refreshTokens.Verify(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplayingTheSameToken_ShouldBeRejectedAfterRotation()
    {
        // Arrange
        var stored = AStoredToken();
        _refreshTokens.Setup(repository => repository.GetByTokenHash("hashed-refresh", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Act
        var replay = async () => await AUseCase().Execute("raw-refresh", CancellationToken.None);

        // Assert
        await replay.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task FailedRefresh_ShouldNotCommitAnything()
    {
        // Arrange
        _refreshTokens.Setup(repository => repository.GetByTokenHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        await Record.ExceptionAsync(() => AUseCase().Execute("raw-refresh", CancellationToken.None));

        // Assert
        _refreshTokens.Verify(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

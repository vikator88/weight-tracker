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

public class LoginUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public LoginUseCaseTests()
    {
        _clock.SetupGet(clock => clock.UtcNow).Returns(Now);
        _tokens.Setup(service => service.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new GeneratedAccessToken("access-token", 900));
        _tokens.Setup(service => service.GenerateRefreshToken())
            .Returns(new GeneratedRefreshToken("raw-refresh", "hashed-refresh", Now.AddDays(30)));
    }

    private LoginUseCase AUseCase() => new(
        _users.Object, _refreshTokens.Object, _passwordHasher.Object,
        _tokens.Object, _clock.Object, _unitOfWork.Object);

    private static User AUser() => User.Rehydrate(
        Id.New(), Email.From("user@weighttracker.test"), "Ada", "Lovelace",
        new DateOnly(1988, 5, 12), Role.USER, "stored-hash", Now);

    [Fact]
    public async Task UnknownEmail_ShouldBeRejectedAsInvalidCredentials()
    {
        // Arrange
        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = async () => await AUseCase().Execute("missing@weighttracker.test", "Passw0rd!", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task WrongPassword_ShouldBeRejectedAsInvalidCredentials()
    {
        // Arrange
        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(AUser());
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        var act = async () => await AUseCase().Execute("user@weighttracker.test", "wrong", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task MalformedEmail_ShouldBeRejectedAsInvalidCredentials()
    {
        // Arrange & Act
        var act = async () => await AUseCase().Execute("not-an-email", "Passw0rd!", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _users.Verify(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnknownEmailAndWrongPassword_ShouldFailIdentically()
    {
        // Arrange
        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var unknown = await Record.ExceptionAsync(
            () => AUseCase().Execute("missing@weighttracker.test", "Passw0rd!", CancellationToken.None));

        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(AUser());
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        var wrongPassword = await Record.ExceptionAsync(
            () => AUseCase().Execute("user@weighttracker.test", "wrong", CancellationToken.None));

        // Assert
        unknown!.GetType().Should().Be(wrongPassword!.GetType());
        unknown.Message.Should().Be(wrongPassword.Message);
    }

    [Fact]
    public async Task ValidCredentials_ShouldReturnTheGeneratedPair()
    {
        // Arrange
        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(AUser());
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Act
        var result = await AUseCase().Execute("user@weighttracker.test", "Passw0rd!", CancellationToken.None);

        // Assert
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh");
        result.ExpiresIn.Should().Be(900);
    }

    [Fact]
    public async Task ValidCredentials_ShouldPersistExactlyOneRefreshTokenHash()
    {
        // Arrange
        var user = AUser();
        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        RefreshToken? saved = null;
        _refreshTokens.Setup(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => saved = token)
            .Returns(Task.CompletedTask);

        // Act
        await AUseCase().Execute("user@weighttracker.test", "Passw0rd!", CancellationToken.None);

        // Assert
        _refreshTokens.Verify(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        saved.Should().NotBeNull();
        saved!.UserId.Should().Be(user.Id);
        saved.TokenHash.Should().Be("hashed-refresh");
        saved.TokenHash.Should().NotBe("raw-refresh");
        saved.ExpiresAt.Should().Be(Now.AddDays(30));
        saved.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public async Task ValidCredentials_ShouldCommitTheUnitOfWork()
    {
        // Arrange
        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(AUser());
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Act
        await AUseCase().Execute("user@weighttracker.test", "Passw0rd!", CancellationToken.None);

        // Assert
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailedLogin_ShouldNotCommitAnything()
    {
        // Arrange
        _users.Setup(repository => repository.GetByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        await Record.ExceptionAsync(() => AUseCase().Execute("missing@weighttracker.test", "Passw0rd!", CancellationToken.None));

        // Assert
        _refreshTokens.Verify(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

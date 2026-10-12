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

public class SignupUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private const string ValidEmail = "new.user@weighttracker.test";
    private const string ValidPassword = "Passw0rd!";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public SignupUseCaseTests()
    {
        _clock.SetupGet(clock => clock.UtcNow).Returns(Now);
        _users.Setup(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<Password>())).Returns(PasswordHash.From("stored-hash"));
        _tokens.Setup(service => service.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new GeneratedAccessToken("access-token", 900));
        _tokens.Setup(service => service.GenerateRefreshToken())
            .Returns(new GeneratedRefreshToken("raw-refresh", "hashed-refresh", Now.AddDays(30)));
    }

    private SignupUseCase AUseCase() => new(
        _users.Object, _refreshTokens.Object, _passwordHasher.Object,
        _tokens.Object, _clock.Object, _unitOfWork.Object);

    private void GivenTheEmailIsAlreadyRegistered()
        => _users.Setup(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

    [Fact]
    public async Task ValidInput_ShouldReturnTheGeneratedPair()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var result = await useCase.Execute(ValidEmail, ValidPassword, Role.USER, CancellationToken.None);

        // Assert
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("raw-refresh");
        result.ExpiresIn.Should().Be(900);
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    public async Task ValidInput_ShouldSaveANewUserWithTheRequestedRoleAndHashedPassword(Role role)
    {
        // Arrange
        User? saved = null;
        _users.Setup(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => saved = user)
            .Returns(Task.CompletedTask);

        // Act
        await AUseCase().Execute(ValidEmail, ValidPassword, role, CancellationToken.None);

        // Assert
        saved.Should().NotBeNull();
        saved!.IsNew.Should().BeTrue();
        saved.Email.Should().Be(Email.From(ValidEmail));
        saved.Role.Should().Be(role);
        saved.PasswordHash.Should().Be(PasswordHash.From("stored-hash"));
        saved.CreatedAt.Should().Be(Now);
        saved.Name.Should().Be(PersonName.Empty());
        saved.Surname.Should().Be(PersonName.Empty());
    }

    [Fact]
    public async Task ValidInput_ShouldNeverStoreThePlainTextPassword()
    {
        // Arrange
        User? saved = null;
        _users.Setup(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => saved = user)
            .Returns(Task.CompletedTask);

        // Act
        await AUseCase().Execute(ValidEmail, ValidPassword, Role.USER, CancellationToken.None);

        // Assert
        _passwordHasher.Verify(hasher => hasher.Hash(It.Is<Password>(password => password.Value == ValidPassword)), Times.Once);
        saved!.PasswordHash.Value.Should().NotBe(ValidPassword);
    }

    [Fact]
    public async Task ValidInput_ShouldPersistExactlyOneRefreshTokenHash()
    {
        // Arrange
        User? savedUser = null;
        _users.Setup(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => savedUser = user)
            .Returns(Task.CompletedTask);

        RefreshToken? savedToken = null;
        _refreshTokens.Setup(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => savedToken = token)
            .Returns(Task.CompletedTask);

        // Act
        await AUseCase().Execute(ValidEmail, ValidPassword, Role.USER, CancellationToken.None);

        // Assert
        _refreshTokens.Verify(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
        savedToken.Should().NotBeNull();
        savedToken!.UserId.Should().Be(savedUser!.Id);
        savedToken.TokenHash.Should().Be("hashed-refresh");
        savedToken.TokenHash.Should().NotBe("raw-refresh");
        savedToken.ExpiresAt.Should().Be(Now.AddDays(30));
        savedToken.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public async Task ValidInput_ShouldCommitTheUnitOfWork()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        await useCase.Execute(ValidEmail, ValidPassword, Role.USER, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistingEmail_ShouldBeRejectedAsEmailAlreadyExists()
    {
        // Arrange
        GivenTheEmailIsAlreadyRegistered();
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(ValidEmail, ValidPassword, Role.USER, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAlreadyExistException>();
    }

    [Fact]
    public async Task ExistingEmail_ShouldNotCommitAnything()
    {
        // Arrange
        GivenTheEmailIsAlreadyRegistered();
        var useCase = AUseCase();

        // Act
        await Record.ExceptionAsync(() => useCase.Execute(ValidEmail, ValidPassword, Role.USER, CancellationToken.None));

        // Assert
        _users.Verify(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokens.Verify(repository => repository.Save(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Ab1!")]
    [InlineData("passw0rd!")]
    [InlineData("Password!")]
    [InlineData("Passw0rdd")]
    public async Task WeakPassword_ShouldBeRejectedAsNotStrongEnough(string weakPassword)
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(ValidEmail, weakPassword, Role.USER, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PasswordNotStrongEnoughException>();
    }

    [Fact]
    public async Task WeakPassword_ShouldNotTouchTheRepositoriesOrTheHasher()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        await Record.ExceptionAsync(() => useCase.Execute(ValidEmail, "weak", Role.USER, CancellationToken.None));

        // Assert
        _users.Verify(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<Password>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MalformedEmail_ShouldBeRejectedAsInvalidCredentials()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute("not-an-email", ValidPassword, Role.USER, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _users.Verify(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdminRole_ShouldBeRejectedAsAdminSignupNotAllowed()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(ValidEmail, ValidPassword, Role.ADMIN, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AdminSignupNotAllowedException>();
    }

    [Fact]
    public async Task AdminRole_ShouldNotTouchTheRepositoriesOrTheHasher()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        await Record.ExceptionAsync(() => useCase.Execute(ValidEmail, ValidPassword, Role.ADMIN, CancellationToken.None));

        // Assert
        _users.Verify(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
        _users.Verify(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<Password>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdminRole_ShouldBeRejectedBeforeValidatingTheCredentials()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute("not-an-email", "weak", Role.ADMIN, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AdminSignupNotAllowedException>();
    }
}

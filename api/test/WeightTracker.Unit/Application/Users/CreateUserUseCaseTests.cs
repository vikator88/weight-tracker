using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Users;

public class CreateUserUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private const string ValidEmail = "new.user@weighttracker.test";
    private const string ValidPassword = "Passw0rd!";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public CreateUserUseCaseTests()
    {
        _clock.SetupGet(clock => clock.UtcNow).Returns(Now);
        _users.Setup(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<Password>())).Returns(PasswordHash.From("stored-hash"));
    }

    private CreateUserUseCase AUseCase() => new(
        _users.Object, _passwordHasher.Object, _clock.Object, _unitOfWork.Object);

    private static CreateUserDTO ARequest(
        string email = ValidEmail,
        string password = ValidPassword,
        Role role = Role.USER,
        string? name = null,
        string? surname = null,
        DateOnly? dateBirth = null)
        => new(email, password, role, name, surname, dateBirth);

    private void GivenTheEmailIsAlreadyRegistered()
        => _users.Setup(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

    private void ThenNothingWasPersisted()
    {
        _users.Verify(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task ValidInput_ShouldSaveANewUserWithTheRequestedRoleAndHashedPassword(Role role)
    {
        // Arrange
        User? saved = null;
        _users.Setup(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => saved = user)
            .Returns(Task.CompletedTask);

        // Act
        var result = await AUseCase().Execute(ARequest(role: role), CancellationToken.None);

        // Assert
        saved.Should().NotBeNull();
        saved.Should().BeSameAs(result);
        saved!.IsNew.Should().BeTrue();
        saved.Email.Should().Be(Email.From(ValidEmail));
        saved.Role.Should().Be(role);
        saved.PasswordHash.Should().Be(PasswordHash.From("stored-hash"));
        saved.CreatedAt.Should().Be(Now);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OmittedOptionalFields_ShouldStoreEmptyNamesAndTheUnsetDateOfBirth()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var user = await useCase.Execute(ARequest(), CancellationToken.None);

        // Assert
        user.Name.Should().Be(PersonName.Empty());
        user.Surname.Should().Be(PersonName.Empty());
        user.DateBirth.Should().Be(DateOnly.MinValue);
    }

    [Fact]
    public async Task GivenOptionalFields_ShouldStoreThemWithTrimmedNames()
    {
        // Arrange
        var useCase = AUseCase();
        var dateBirth = new DateOnly(1990, 5, 17);

        // Act
        var user = await useCase.Execute(
            ARequest(name: "  Ana ", surname: " García  ", dateBirth: dateBirth), CancellationToken.None);

        // Assert
        user.Name.Value.Should().Be("Ana");
        user.Surname.Value.Should().Be("García");
        user.DateBirth.Should().Be(dateBirth);
    }

    [Fact]
    public async Task MalformedEmail_ShouldBeRejectedAsInvalidEmail()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(ARequest(email: "not-an-email"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidEmailException>();
        _users.Verify(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
        ThenNothingWasPersisted();
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
        var act = async () => await useCase.Execute(ARequest(password: weakPassword), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PasswordNotStrongEnoughException>();
        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<Password>()), Times.Never);
        ThenNothingWasPersisted();
    }

    [Theory]
    [InlineData("   ", null)]
    [InlineData(null, "")]
    public async Task InvalidName_ShouldBeRejectedAsInvalidPersonName(string? name, string? surname)
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(ARequest(name: name, surname: surname), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPersonNameException>();
        _users.Verify(repository => repository.ExistsByEmail(It.IsAny<Email>(), It.IsAny<CancellationToken>()), Times.Never);
        ThenNothingWasPersisted();
    }

    [Fact]
    public async Task TooLongName_ShouldBeRejectedAsInvalidPersonName()
    {
        // Arrange
        var useCase = AUseCase();
        var tooLong = new string('a', PersonName.MaxLength + 1);

        // Act
        var act = async () => await useCase.Execute(ARequest(name: tooLong), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPersonNameException>();
        ThenNothingWasPersisted();
    }

    [Fact]
    public async Task ExistingEmail_ShouldBeRejectedAsEmailAlreadyExists()
    {
        // Arrange
        GivenTheEmailIsAlreadyRegistered();
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(ARequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EmailAlreadyExistException>();
        _passwordHasher.Verify(hasher => hasher.Hash(It.IsAny<Password>()), Times.Never);
        ThenNothingWasPersisted();
    }
}

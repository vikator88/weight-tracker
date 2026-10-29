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

public class UpdateMyProfileUseCaseTests
{
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private UpdateMyProfileUseCase AUseCase() => new(_users.Object, _unitOfWork.Object);

    private static User AStoredUser() => User.Rehydrate(
        Id.New(),
        Email.From("user@weighttracker.test"),
        PersonName.From("Ada"),
        PersonName.From("Lovelace"),
        new DateOnly(1990, 4, 12),
        Role.USER,
        PasswordHash.From("stored-hash"),
        new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
        true,
        null);

    [Fact]
    public async Task Execute_ShouldReplaceNameAndSurnameAndCommitOnce()
    {
        // Arrange
        var stored = AStoredUser();
        _users.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _users.Setup(repository => repository.Save(stored, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await AUseCase().Execute(stored.Id, new UpdateProfileDTO("  Grace ", "Hopper"), CancellationToken.None);

        // Assert
        result.Name.Value.Should().Be("Grace");
        result.Surname.Value.Should().Be("Hopper");
        result.Email.Value.Should().Be("user@weighttracker.test");
        result.Role.Should().Be(Role.USER);
        _users.Verify(repository => repository.Save(stored, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Hopper")]
    [InlineData("   ", "Hopper")]
    [InlineData("Grace", "")]
    [InlineData("Grace", "   ")]
    public async Task Execute_ShouldRejectABlankNameOrSurnameBeforeReadingOrSaving(string name, string surname)
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = () => useCase.Execute(Id.New(), new UpdateProfileDTO(name, surname), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPersonNameException>();
        _users.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectATooLongSurnameBeforeReadingOrSaving()
    {
        // Arrange
        var useCase = AUseCase();
        var tooLong = new string('a', PersonName.MaxLength + 1);

        // Act
        var act = () => useCase.Execute(Id.New(), new UpdateProfileDTO("Grace", tooLong), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPersonNameException>();
        _users.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownUserWithoutSavingOrCommitting()
    {
        // Arrange
        var id = Id.New();
        _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => AUseCase().Execute(id, new UpdateProfileDTO("Grace", "Hopper"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticatedUserNotFoundException>();
        _users.Verify(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }
}

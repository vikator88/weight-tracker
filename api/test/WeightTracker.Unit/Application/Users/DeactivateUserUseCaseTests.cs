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

public class DeactivateUserUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Id AdminId = Id.New();

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    public DeactivateUserUseCaseTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(Now);
    }

    private static User AStoredUser(bool isActive = true) => User.Rehydrate(
        Id.New(), Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.USER, PasswordHash.From("stored-hash"), Now.AddDays(-30),
        isActive, isActive ? null : Now.AddDays(-1));

    private void GivenStored(User user)
        => _users.Setup(repository => repository.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

    private void GivenSaveAndCommitSucceed(User user)
    {
        _users.Setup(repository => repository.Save(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _users.Verify(repository => repository.Save(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private DeactivateUserUseCase AUseCase() => new(_users.Object, _clock.Object, _unitOfWork.Object);

    [Fact]
    public async Task Execute_ShouldDeactivateSaveAndCommitOnce()
    {
        // Arrange
        var user = AStoredUser();
        GivenStored(user);
        GivenSaveAndCommitSucceed(user);

        // Act
        var deactivated = await AUseCase().Execute(AdminId, Role.ADMIN, user.Id, CancellationToken.None);

        // Assert
        deactivated.IsActive.Should().BeFalse();
        deactivated.DeactivatedAtUtc.Should().Be(Now);
        _users.Verify(repository => repository.Save(user, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(Role.USER)]
    [InlineData(Role.TRAINER)]
    public async Task Execute_ShouldRejectANonAdminWithoutLookingTheUserUp(Role callerRole)
    {
        // Arrange
        var userId = Id.New();

        // Act
        var act = () => AUseCase().Execute(Id.New(), callerRole, userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminDeactivatingThemselvesWithoutLookingThemUp()
    {
        // Arrange
        var callerId = AdminId;

        // Act
        var act = () => AUseCase().Execute(callerId, Role.ADMIN, callerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldThrowWhenTheUserDoesNotExist()
    {
        // Arrange
        var userId = Id.New();
        _users.Setup(repository => repository.GetById(userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAlreadyDeactivatedUserWithoutSaving()
    {
        // Arrange
        var user = AStoredUser(isActive: false);
        GivenStored(user);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, user.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserAlreadyDeactivatedException>();
        VerifyNothingSavedOrCommitted();
    }
}

using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutAssignmentValidatorTests
{
    private static readonly Id UserId = Id.New();

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private WorkoutAssignmentValidator AValidator() => new(_users.Object);

    private static User AUser(Id id, Role role) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Grace"), PersonName.From("Hopper"),
        new DateOnly(1985, 12, 9), role, PasswordHash.From("stored-hash"), new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task EnsureOwnerExists_ShouldReturnTheStoredOwner()
    {
        // Arrange
        _users.Setup(repository => repository.GetById(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AUser(UserId, Role.USER));

        // Act
        var owner = await AValidator().EnsureOwnerExists(UserId, CancellationToken.None);

        // Assert
        owner.Id.Should().Be(UserId);
    }

    [Fact]
    public async Task EnsureOwnerExists_ShouldRejectAMissingOwner()
    {
        // Arrange
        _users.Setup(repository => repository.GetById(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => AValidator().EnsureOwnerExists(UserId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task EnsureCanTrain_ShouldAcceptATrainerOrAnAdmin()
    {
        // Arrange
        _users.Setup(repository => repository.IsTrainerOrAdmin(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => AValidator().EnsureCanTrain(UserId, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanTrain_ShouldRejectAUserWhoCannotTrainOrDoesNotExist()
    {
        // Arrange
        _users.Setup(repository => repository.IsTrainerOrAdmin(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = () => AValidator().EnsureCanTrain(UserId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
    }
}

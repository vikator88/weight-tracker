using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class GetAllWorkoutsUseCaseTests
{
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private GetAllWorkoutsUseCase AUseCase() => new(_workouts.Object);

    private static Workout AWorkout(Id ownerId) =>
        Workout.Rehydrate(Id.New(), ownerId, null, new DateTime(2026, 10, 1), new());

    [Fact]
    public async Task Admin_ShouldSeeEveryWorkout()
    {
        // Arrange
        var callerId = Id.New();
        var all = new[] { AWorkout(Id.New()), AWorkout(Id.New()) };
        _workouts.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        // Act
        var result = await AUseCase().Execute(callerId, Role.ADMIN, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(all);
        _workouts.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Trainer_ShouldSeeOwnedAndTrainedWorkouts()
    {
        // Arrange
        var callerId = Id.New();
        var visible = new[] { AWorkout(callerId) };
        _workouts.Setup(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync(visible);

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(visible);
        _workouts.Verify(repository => repository.GetAllByUserOrTrainerId(callerId, It.IsAny<CancellationToken>()), Times.Once);
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task User_ShouldOnlySeeOwnedWorkouts()
    {
        // Arrange
        var callerId = Id.New();
        var owned = new[] { AWorkout(callerId) };
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync(owned);

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(owned);
        _workouts.Verify(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()), Times.Once);
        _workouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnknownRole_ShouldBeRejected()
    {
        // Arrange & Act
        var act = async () => await AUseCase().Execute(Id.New(), (Role)99, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _workouts.VerifyNoOtherCalls();
    }
}

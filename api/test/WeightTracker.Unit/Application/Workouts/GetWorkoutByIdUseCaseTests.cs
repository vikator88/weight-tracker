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

public class GetWorkoutByIdUseCaseTests
{
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private GetWorkoutByIdUseCase AUseCase() => new(_workouts.Object);

    private Workout StoredWorkout(Id ownerId, Id? trainerId = null)
    {
        var workout = Workout.Rehydrate(Id.New(), ownerId, trainerId, new DateTime(2026, 10, 1), []);
        _workouts
            .Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workout);

        return workout;
    }

    [Fact]
    public async Task Admin_ShouldReadAnyWorkout()
    {
        // Arrange
        var workout = StoredWorkout(Id.New(), Id.New());

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Trainer_ShouldReadAWorkoutTheyOwn()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId);

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Trainer_ShouldReadAWorkoutTheyPrescribed()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(Id.New(), callerId);

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task Trainer_ShouldNotReadAnUnrelatedWorkout()
    {
        // Arrange
        var workout = StoredWorkout(Id.New(), Id.New());

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.TRAINER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task User_ShouldReadAWorkoutTheyOwn()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(callerId, Id.New());

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(workout);
    }

    [Fact]
    public async Task User_ShouldNotReadAnotherUsersWorkout()
    {
        // Arrange
        var callerId = Id.New();
        var workout = StoredWorkout(Id.New(), callerId);

        // Act
        var act = () => AUseCase().Execute(callerId, Role.USER, workout.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task ShouldFailWhenTheWorkoutDoesNotExist()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts
            .Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.ADMIN, workoutId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
    }
}

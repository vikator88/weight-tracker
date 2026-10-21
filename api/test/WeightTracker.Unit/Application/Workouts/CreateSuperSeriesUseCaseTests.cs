using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class CreateSuperSeriesUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private CreateSuperSeriesUseCase AUseCase() => new(_workouts.Object, new WorkoutEditAuthorizer(), _unitOfWork.Object);

    /// <summary>A stored workout prescribed by the trainer, with two entries of three sets and one of four.</summary>
    private static Workout AStoredWorkout(out Id[] entryIds)
    {
        var threeSets = new SetPrescription(3, new SetTarget.Reps(10));
        entryIds = [Id.New(), Id.New(), Id.New()];

        return Workout.Rehydrate(Id.New(), OwnerId, TrainerId, WorkoutDate, WorkoutStatus.DRAFT,
        [
            WorkoutExercise.Rehydrate(entryIds[0], Id.New(), Effort.MID, [threeSets]),
            WorkoutExercise.Rehydrate(entryIds[1], Id.New(), Effort.MID, [threeSets]),
            WorkoutExercise.Rehydrate(entryIds[2], Id.New(), Effort.MID, [new SetPrescription(4, new SetTarget.Reps(8))]),
        ]);
    }

    private void GivenStored(Workout workout)
        => _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

    private void VerifyNothingSavedOrCommitted()
    {
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldGroupTheEntriesAndCommitOnce()
    {
        // Arrange
        var workout = AStoredWorkout(out var entryIds);
        GivenStored(workout);
        _workouts.Setup(repository => repository.Save(workout, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var superSeriesId = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, [entryIds[0], entryIds[1]], CancellationToken.None);

        // Assert
        workout.Exercises.Where(entry => entry.SuperSeriesId == superSeriesId).Select(entry => entry.Id)
            .Should().BeEquivalentTo([entryIds[0], entryIds[1]]);
        workout.Exercises.Single(entry => entry.Id == entryIds[2]).SuperSeriesId.Should().BeNull();
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, [Id.New(), Id.New()], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectACallerWhoMayNotEditTheWorkoutWithoutGroupingAnything()
    {
        // Arrange
        var workout = AStoredWorkout(out var entryIds);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, [entryIds[0], entryIds[1]], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnotherTrainerOnAPrescribedWorkout()
    {
        // Arrange
        var workout = AStoredWorkout(out var entryIds);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.TRAINER, workout.Id, [entryIds[0], entryIds[1]], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateADomainErrorWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(out var entryIds);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, [entryIds[0], entryIds[2]], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SuperSeriesSetCountMismatchException>();
        VerifyNothingSavedOrCommitted();
    }
}

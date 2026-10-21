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

public class BreakSuperSeriesUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Id OwnerId = Id.New();

    private const int StoredSuperSeries = 1;

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private BreakSuperSeriesUseCase AUseCase() => new(_workouts.Object, new WorkoutEditAuthorizer(), _unitOfWork.Object);

    /// <summary>A stored workout created by its owner, with its two entries grouped in superseries 1.</summary>
    private static Workout AStoredWorkout()
    {
        var threeSets = new SetPrescription(3, new SetTarget.Reps(10));
        var superSeriesId = SuperSeriesId.From(StoredSuperSeries);

        return Workout.Rehydrate(Id.New(), OwnerId, null, WorkoutDate, WorkoutStatus.DRAFT,
        [
            WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [threeSets], superSeriesId),
            WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [threeSets], superSeriesId),
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
    public async Task Execute_ShouldUngroupTheEntriesAndCommitOnce()
    {
        // Arrange
        var workout = AStoredWorkout();
        var entryIds = workout.Exercises.Select(entry => entry.Id).ToList();
        GivenStored(workout);
        _workouts.Setup(repository => repository.Save(workout, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await AUseCase().Execute(OwnerId, Role.USER, workout.Id, StoredSuperSeries, CancellationToken.None);

        // Assert
        workout.Exercises.Select(entry => entry.Id).Should().Equal(entryIds);
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnInvalidSuperSeriesIdBeforeLoadingTheWorkout()
    {
        // Arrange
        var workoutId = Id.New();

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workoutId, 0, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidSuperSeriesIdException>();
        _workouts.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workoutId, StoredSuperSeries, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectACallerWhoMayNotEditTheWorkoutAndKeepTheSuperSeries()
    {
        // Arrange
        var workout = AStoredWorkout();
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.TRAINER, workout.Id, StoredSuperSeries, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == SuperSeriesId.From(StoredSuperSeries));
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateAnUnknownSuperSeriesWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout();
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, StoredSuperSeries + 1, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<SuperSeriesNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }
}

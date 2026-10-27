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

public class ReorderWorkoutExercisesUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private ReorderWorkoutExercisesUseCase AUseCase() => new(_workouts.Object, new WorkoutEditAuthorizer(), _unitOfWork.Object);

    /// <summary>A stored workout with three catalog exercises, prescribed by the trainer unless told otherwise.</summary>
    private static Workout AStoredWorkout(out Id[] exerciseIds, WorkoutStatus status = WorkoutStatus.DRAFT, bool prescribed = true)
    {
        var threeSets = new SetPrescription(3, new SetTarget.Reps(10));
        exerciseIds = [Id.New(), Id.New(), Id.New()];

        return Workout.Rehydrate(Id.New(), OwnerId, prescribed ? TrainerId : null, Name, null, WorkoutDate, status,
            exerciseIds.Select(exerciseId => WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [threeSets])));
    }

    private void GivenStored(Workout workout)
        => _workouts.Setup(repository => repository.GetById(workout.Id, It.IsAny<CancellationToken>())).ReturnsAsync(workout);

    private void GivenSaveAndCommitSucceed(Workout workout)
    {
        _workouts.Setup(repository => repository.Save(workout, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private static IEnumerable<Id> OrderOf(Workout workout) => workout.Exercises.Select(entry => entry.ExerciseId);

    [Fact]
    public async Task Execute_ShouldReorderTheExercisesAndCommitOnce()
    {
        // Arrange
        var workout = AStoredWorkout(out var ids);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        var result = await AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, [ids[2], ids[0], ids[1]], CancellationToken.None);

        // Assert
        result.Should().BeSameAs(workout);
        OrderOf(workout).Should().Equal(ids[2], ids[0], ids[1]);
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldLetTheOwnerReorderAWorkoutWithoutTrainer()
    {
        // Arrange
        var workout = AStoredWorkout(out var ids, prescribed: false);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(OwnerId, Role.USER, workout.Id, [ids[1], ids[2], ids[0]], CancellationToken.None);

        // Assert
        OrderOf(workout).Should().Equal(ids[1], ids[2], ids[0]);
    }

    [Fact]
    public async Task Execute_ShouldLetAnAdminReorderAStartedWorkout()
    {
        // Arrange
        var workout = AStoredWorkout(out var ids, WorkoutStatus.STARTED);
        GivenStored(workout);
        GivenSaveAndCommitSucceed(workout);

        // Act
        await AUseCase().Execute(Id.New(), Role.ADMIN, workout.Id, [ids[1], ids[0], ids[2]], CancellationToken.None);

        // Assert
        OrderOf(workout).Should().Equal(ids[1], ids[0], ids[2]);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownWorkoutWithoutSavingOrCommitting()
    {
        // Arrange
        var workoutId = Id.New();
        _workouts.Setup(repository => repository.GetById(workoutId, It.IsAny<CancellationToken>())).ReturnsAsync((Workout?)null);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workoutId, [Id.New()], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectTheOwnerOfAPrescribedWorkoutWithoutReordering()
    {
        // Arrange
        var workout = AStoredWorkout(out var ids);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(OwnerId, Role.USER, workout.Id, [ids[2], ids[1], ids[0]], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        OrderOf(workout).Should().Equal(ids);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnotherTrainerOnAPrescribedWorkout()
    {
        // Arrange
        var workout = AStoredWorkout(out var ids);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(Id.New(), Role.TRAINER, workout.Id, [ids[2], ids[1], ids[0]], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        OrderOf(workout).Should().Equal(ids);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectThePrescribingTrainerOnAStartedWorkout()
    {
        // Arrange
        var workout = AStoredWorkout(out var ids, WorkoutStatus.STARTED);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, [ids[2], ids[1], ids[0]], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutLockedException>();
        OrderOf(workout).Should().Equal(ids);
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateAMismatchingOrderWithoutSavingOrCommitting()
    {
        // Arrange
        var workout = AStoredWorkout(out var ids);
        GivenStored(workout);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, workout.Id, [ids[1], ids[0]], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WorkoutExerciseOrderMismatchException>();
        OrderOf(workout).Should().Equal(ids);
        VerifyNothingSavedOrCommitted();
    }
}

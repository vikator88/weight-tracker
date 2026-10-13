using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class GetAllWorkoutsUseCaseTests
{
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private GetAllWorkoutsUseCase AUseCase() => new(_workouts.Object, _exercises.Object);

    private static Workout AWorkout(Id ownerId, params Id[] exerciseIds) =>
        Workout.Rehydrate(
            Id.New(), ownerId, null, new DateTime(2026, 10, 1),
            exerciseIds.Select(exerciseId => WorkoutExercise.Rehydrate(
                Id.New(), exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])));

    private static Exercise AnExercise(Id id) =>
        Exercise.Rehydrate(id, ExerciseName.From("Press Banca"), null, BodyParts.CHEST);

    private void NoExercisesStored() =>
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

    [Fact]
    public async Task Admin_ShouldSeeEveryWorkout()
    {
        // Arrange
        var callerId = Id.New();
        var all = new[] { AWorkout(Id.New()), AWorkout(Id.New()) };
        _workouts.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);
        NoExercisesStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.ADMIN, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo(all);
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
        NoExercisesStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.TRAINER, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo(visible);
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
        NoExercisesStored();

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Workouts.Should().BeEquivalentTo(owned);
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
        _exercises.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldLoadEachReferencedExerciseOnce()
    {
        // Arrange
        var callerId = Id.New();
        var pressBanca = Id.New();
        var sentadilla = Id.New();
        var owned = new[] { AWorkout(callerId, pressBanca, sentadilla), AWorkout(callerId, pressBanca) };
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>())).ReturnsAsync(owned);
        NoExercisesStored();

        // Act
        await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        _exercises.Verify(repository => repository.GetAllByIds(
            It.Is<IEnumerable<Id>>(ids => ids.Count() == 2 && ids.Contains(pressBanca) && ids.Contains(sentadilla)),
            It.IsAny<CancellationToken>()), Times.Once);
        _exercises.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnTheReferencedExercisesById()
    {
        // Arrange
        var callerId = Id.New();
        var pressBancaId = Id.New();
        var pressBanca = AnExercise(pressBancaId);
        _workouts.Setup(repository => repository.GetAllByUserId(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([AWorkout(callerId, pressBancaId)]);
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([pressBanca]);

        // Act
        var result = await AUseCase().Execute(callerId, Role.USER, CancellationToken.None);

        // Assert
        result.Exercises.Should().ContainSingle();
        result.Exercises[pressBancaId].Should().BeSameAs(pressBanca);
    }
}

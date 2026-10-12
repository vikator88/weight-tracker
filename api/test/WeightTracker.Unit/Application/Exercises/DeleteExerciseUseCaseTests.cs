using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Application.Exercises;

public class DeleteExerciseUseCaseTests
{
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);
    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private DeleteExerciseUseCase AUseCase() => new(_exercises.Object, _workouts.Object, _unitOfWork.Object);

    private static Exercise AStoredExercise() =>
        Exercise.Rehydrate(Id.New(), ExerciseName.From("Curl de Bíceps"), null, BodyParts.ARMS);

    [Fact]
    public async Task Execute_ShouldStageTheDeletionAndCommitOnceWhenNoWorkoutUsesTheExercise()
    {
        // Arrange
        var stored = AStoredExercise();
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _workouts.Setup(repository => repository.ExistsByExerciseId(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _exercises.Setup(repository => repository.Delete(stored, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await AUseCase().Execute(stored.Id, CancellationToken.None);

        // Assert
        _exercises.Verify(repository => repository.Delete(stored, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnExerciseIncludedInAWorkoutWithoutDeletingOrCommitting()
    {
        // Arrange
        var stored = AStoredExercise();
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _workouts.Setup(repository => repository.ExistsByExerciseId(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = () => AUseCase().Execute(stored.Id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseInUseException>();
        _exercises.Verify(repository => repository.Delete(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownExerciseWithoutDeletingOrCommitting()
    {
        // Arrange
        var id = Id.New();
        _exercises.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AUseCase().Execute(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        _workouts.VerifyNoOtherCalls();
        _exercises.Verify(repository => repository.Delete(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }
}

using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Application.Exercises;

public class UpdateExerciseUseCaseTests
{
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private UpdateExerciseUseCase AUseCase() => new(_exercises.Object, _unitOfWork.Object);

    private static Exercise AStoredExercise() => Exercise.Rehydrate(
        Id.New(),
        ExerciseName.From("Press Banca"),
        Url.From("https://videos.example.com/press-banca"),
        BodyParts.CHEST);

    [Fact]
    public async Task Execute_ShouldReplaceEveryAttributeAndCommitOnce()
    {
        // Arrange
        var stored = AStoredExercise();
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _exercises.Setup(repository => repository.Save(stored, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var dto = new ExerciseDTO("Sentadilla", "https://videos.example.com/sentadilla", BodyParts.LEGS);

        // Act
        var result = await AUseCase().Execute(stored.Id, dto, CancellationToken.None);

        // Assert
        result.Id.Should().Be(stored.Id);
        result.Name.Value.Should().Be("Sentadilla");
        result.VideoUrl!.Value.Should().Be("https://videos.example.com/sentadilla");
        result.BodyPart.Should().Be(BodyParts.LEGS);
        _exercises.Verify(repository => repository.Save(stored, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldClearTheVideoUrlWhenNoneIsGiven()
    {
        // Arrange
        var stored = AStoredExercise();
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _exercises.Setup(repository => repository.Save(stored, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await AUseCase().Execute(stored.Id, new ExerciseDTO("Press Banca", null, BodyParts.CHEST), CancellationToken.None);

        // Assert
        result.VideoUrl.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownExerciseWithoutSavingOrCommitting()
    {
        // Arrange
        var id = Id.New();
        _exercises.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AUseCase().Execute(id, new ExerciseDTO("Sentadilla", null, BodyParts.LEGS), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
        _exercises.Verify(repository => repository.Save(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Execute_ShouldRejectInvalidInputWithoutSavingOrCommitting()
    {
        // Arrange
        var stored = AStoredExercise();
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        // Act
        var act = () => AUseCase().Execute(stored.Id, new ExerciseDTO("ab", null, BodyParts.CHEST), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidExerciseNameException>();
        _exercises.Verify(repository => repository.Save(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }
}

using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Application.Exercises;

public class CreateExerciseUseCaseTests
{
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private CreateExerciseUseCase AUseCase() => new(_exercises.Object, _unitOfWork.Object);

    [Fact]
    public async Task Execute_ShouldStageTheNewExerciseAndCommitOnce()
    {
        // Arrange
        Exercise? saved = null;
        _exercises
            .Setup(repository => repository.Save(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()))
            .Callback<Exercise, CancellationToken>((exercise, _) => saved = exercise)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var dto = new ExerciseDTO("Press Banca", "https://videos.example.com/press-banca", BodyParts.CHEST);

        // Act
        var result = await AUseCase().Execute(dto, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(saved);
        result.IsNew.Should().BeTrue();
        result.Name.Value.Should().Be("Press Banca");
        result.VideoUrl!.Value.Should().Be("https://videos.example.com/press-banca");
        result.BodyPart.Should().Be(BodyParts.CHEST);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_ShouldAcceptAMissingVideoUrl()
    {
        // Arrange
        _exercises
            .Setup(repository => repository.Save(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await AUseCase().Execute(new ExerciseDTO("Press Banca", null, BodyParts.CHEST), CancellationToken.None);

        // Assert
        result.VideoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("ab", null, BodyParts.CHEST, typeof(InvalidExerciseNameException))]
    [InlineData("Press Banca", "not a url", BodyParts.CHEST, typeof(InvalidUrlException))]
    [InlineData("Press Banca", null, (BodyParts)99, typeof(InvalidBodyPartException))]
    public async Task Execute_ShouldRejectInvalidInputWithoutPersistingAnything(
        string name, string? videoUrl, BodyParts bodyPart, Type expected)
    {
        // Arrange
        var dto = new ExerciseDTO(name, videoUrl, bodyPart);

        // Act
        var act = () => AUseCase().Execute(dto, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<DomainException>()).Which.Should().BeOfType(expected);
        _exercises.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }
}

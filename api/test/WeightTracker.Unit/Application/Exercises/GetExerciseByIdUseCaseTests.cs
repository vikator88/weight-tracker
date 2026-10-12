using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Application.Exercises;

public class GetExerciseByIdUseCaseTests
{
    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private GetExerciseByIdUseCase AUseCase() => new(_exercises.Object);

    [Fact]
    public async Task Execute_ShouldReturnTheStoredExercise()
    {
        // Arrange
        var stored = Exercise.Rehydrate(Id.New(), ExerciseName.From("Press Banca"), null, BodyParts.CHEST);
        _exercises.Setup(repository => repository.GetById(stored.Id, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        // Act
        var result = await AUseCase().Execute(stored.Id, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnUnknownExercise()
    {
        // Arrange
        var id = Id.New();
        _exercises.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);

        // Act
        var act = () => AUseCase().Execute(id, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExerciseNotFoundException>();
    }
}

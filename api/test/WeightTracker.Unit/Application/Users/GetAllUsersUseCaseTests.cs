using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Users;

public class GetAllUsersUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private GetAllUsersUseCase AUseCase() => new(_users.Object);

    private static User AUser(string email, Role role) => User.Rehydrate(
        Id.New(), Email.From(email), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"), Now, true, null);

    [Fact]
    public async Task Admin_ShouldSeeEveryUser()
    {
        // Arrange
        var all = new[]
        {
            AUser("admin@weighttracker.test", Role.ADMIN),
            AUser("trainer@weighttracker.test", Role.TRAINER),
            AUser("user@weighttracker.test", Role.USER),
        };
        _users.Setup(repository => repository.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        // Act
        var result = await AUseCase().Execute(Id.New(), Role.ADMIN, CancellationToken.None);

        // Assert
        result.Should().Equal(all);
        _users.Verify(repository => repository.GetAll(It.IsAny<CancellationToken>()), Times.Once);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Trainer_ShouldBeRejectedAsNotImplemented()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(Id.New(), Role.TRAINER, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TrainerAthletesNotImplementedException>();
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task User_ShouldBeRejectedAsUnauthorized()
    {
        // Arrange
        var useCase = AUseCase();

        // Act
        var act = async () => await useCase.Execute(Id.New(), Role.USER, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        _users.VerifyNoOtherCalls();
    }
}

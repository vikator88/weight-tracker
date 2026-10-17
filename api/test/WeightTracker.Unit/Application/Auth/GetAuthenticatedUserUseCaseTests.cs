using FluentAssertions;
using Moq;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Application.Auth;

public class GetAuthenticatedUserUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new();

    private GetAuthenticatedUserUseCase AUseCase() => new(_users.Object);

    private static User AUser() => User.Rehydrate(
        Id.New(), Email.From("trainer@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.TRAINER, PasswordHash.From("stored-hash"), Now);

    [Fact]
    public async Task ExistingUser_ShouldBeReturnedWithoutItsPasswordHash()
    {
        // Arrange
        var user = AUser();
        _users.Setup(repository => repository.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await AUseCase().Execute(user.Id, CancellationToken.None);

        // Assert
        result.Should().Be(new AuthenticatedUser(user.Id, user.Email, user.Name, user.Surname, Role.TRAINER));
    }

    [Fact]
    public async Task UnknownUser_ShouldBeRejectedAsAuthenticatedUserNotFound()
    {
        // Arrange
        _users.Setup(repository => repository.GetById(It.IsAny<Id>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = async () => await AUseCase().Execute(Id.New(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthenticatedUserNotFoundException>();
    }
}

using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutEditAuthorizerTests
{
    private const string Operation = "edit workout";

    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Id OwnerId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly WorkoutEditAuthorizer _authorizer = new();

    private static Workout APrescribedWorkout() => Workout.Create(OwnerId, TrainerId, WorkoutDate);

    private static Workout ASelfCreatedWorkout() => Workout.Create(OwnerId, null, WorkoutDate);

    [Fact]
    public void EnsureCanEdit_ShouldAllowAnAdminOnAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowAnAdminOnASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.ADMIN, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowThePrescribingTrainer()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectTheOwnerOfAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectAnotherTrainerOnAPrescribedWorkout()
    {
        // Arrange
        var workout = APrescribedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowAUserWhoOwnsASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.USER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldAllowATrainerWhoOwnsASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, OwnerId, Role.TRAINER, Operation);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectATrainerWhoDoesNotOwnASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, TrainerId, Role.TRAINER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void EnsureCanEdit_ShouldRejectAUserWhoDoesNotOwnASelfCreatedWorkout()
    {
        // Arrange
        var workout = ASelfCreatedWorkout();

        // Act
        var act = () => _authorizer.EnsureCanEdit(workout, Id.New(), Role.USER, Operation);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }
}

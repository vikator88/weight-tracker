using FluentAssertions;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutStatusAuthorizerTests
{
    private const string Operation = "change workout status";

    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Id AthleteId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private readonly WorkoutStatusAuthorizer _authorizer = new();

    private static Workout APrescribedWorkoutIn(WorkoutStatus status)
        => Workout.Rehydrate(Id.New(), AthleteId, TrainerId, WorkoutDate, status, []);

    private static Workout ASelfPlannedWorkoutIn(WorkoutStatus status)
        => Workout.Rehydrate(Id.New(), AthleteId, null, WorkoutDate, status, []);

    private void Ensure(Workout workout, Id callerId, Role role, WorkoutStatus target)
        => _authorizer.EnsureCanChange(workout, callerId, role, target, Operation);

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.FINISHED)]
    [InlineData(WorkoutStatus.FINISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.STARTED)]
    public void Admin_ShouldBeAllowedAnyMove(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, Id.New(), Role.ADMIN, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    public void PrescribingTrainer_ShouldBeAllowedToPublishAndUnpublish(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.FINISHED)]
    public void PrescribingTrainer_ShouldBeRefusedTheMovesThatBelongToTheAthlete(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, to);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.FINISHED)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.DRAFT)]
    public void PrescribingTrainer_ShouldLeaveAMoveOutsideTheLifecycleToTheAggregate(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.FINISHED)]
    public void AthleteOfAPrescribedWorkout_ShouldBeAllowedToMoveItForward(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AthleteOfAPrescribedWorkout_ShouldBeRefusedToUnpublish()
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.PUBLISHED);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, WorkoutStatus.DRAFT);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Theory]
    [InlineData(WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.FINISHED)]
    [InlineData(WorkoutStatus.DRAFT)]
    public void AthleteOfAPrescribedDraft_ShouldBeRefusedWhateverTheTarget(WorkoutStatus target)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, target);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void AthleteOfAPrescribedWorkout_ShouldLeaveAMoveOutsideTheLifecycleToTheAggregate()
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.STARTED);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, WorkoutStatus.PUBLISHED);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.PUBLISHED)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.DRAFT)]
    [InlineData(WorkoutStatus.PUBLISHED, WorkoutStatus.STARTED)]
    [InlineData(WorkoutStatus.STARTED, WorkoutStatus.FINISHED)]
    [InlineData(WorkoutStatus.DRAFT, WorkoutStatus.FINISHED)]
    public void AthleteOfASelfPlannedWorkout_ShouldBeAllowedEveryMove(WorkoutStatus from, WorkoutStatus to)
    {
        // Arrange
        var workout = ASelfPlannedWorkoutIn(from);

        // Act
        var act = () => Ensure(workout, AthleteId, Role.USER, to);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.USER)]
    public void UnrelatedCaller_ShouldBeRefusedEvenForAMoveInsideTheLifecycle(Role role)
    {
        // Arrange
        var workout = APrescribedWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => Ensure(workout, Id.New(), role, WorkoutStatus.PUBLISHED);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void TrainerUnrelatedToASelfPlannedWorkout_ShouldBeRefused()
    {
        // Arrange
        var workout = ASelfPlannedWorkoutIn(WorkoutStatus.DRAFT);

        // Act
        var act = () => Ensure(workout, TrainerId, Role.TRAINER, WorkoutStatus.PUBLISHED);

        // Assert
        act.Should().Throw<UnauthorizedOperationException>();
    }
}

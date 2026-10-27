using FluentAssertions;
using Moq;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Workouts;

public class CreateWorkoutUseCaseTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private static readonly Id UserId = Id.New();

    private static readonly Id TrainerId = Id.New();

    private static readonly Id AdminId = Id.New();

    private readonly Mock<IWorkoutRepository> _workouts = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);

    private CreateWorkoutUseCase AUseCase() => new(_workouts.Object, new WorkoutAssignmentValidator(_users.Object), _unitOfWork.Object);

    private static User AUser(Id id, Role role) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Grace"), PersonName.From("Hopper"),
        new DateOnly(1985, 12, 9), role, PasswordHash.From("stored-hash"), WorkoutDate);

    private void GivenStored(Id id, Role role)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync(AUser(id, role));

    private void GivenNoUser(Id id)
        => _users.Setup(repository => repository.GetById(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

    private void GivenTrainerCheck(Id id, bool canTrain)
        => _users.Setup(repository => repository.IsTrainerOrAdmin(id, It.IsAny<CancellationToken>())).ReturnsAsync(canTrain);

    private void GivenSaveWorks()
    {
        _workouts.Setup(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private void VerifyNothingSavedOrCommitted()
    {
        _workouts.Verify(repository => repository.Save(It.IsAny<Workout>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.VerifyNoOtherCalls();
    }

    private void VerifySavedAndCommittedOnce(Workout workout)
    {
        _workouts.Verify(repository => repository.Save(workout, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyNoUserLookups() => _users.VerifyNoOtherCalls();

    // ---- USER

    [Fact]
    public async Task Execute_ShouldCreateTheWorkoutForTheCallerWhenAUserSendsNoIds()
    {
        // Arrange
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(UserId, Role.USER, null, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().BeNull();
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
        workout.Exercises.Should().BeEmpty();
        VerifySavedAndCommittedOnce(workout);
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldKeepTheNameAndTheDescription()
    {
        // Arrange
        GivenSaveWorks();
        var description = WorkoutDescription.From("Focus on tempo");

        // Act
        var workout = await AUseCase().Execute(UserId, Role.USER, null, null, Name, description, WorkoutDate, CancellationToken.None);

        // Assert
        workout.Name.Should().Be(Name);
        workout.Description.Should().Be(description);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldAcceptTheCallersOwnIdWhenAUserSendsIt()
    {
        // Arrange
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(UserId, Role.USER, UserId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().BeNull();
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectAUserNamingAnotherOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(UserId, Role.USER, Id.New(), null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectAUserNamingATrainer()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(UserId, Role.USER, null, TrainerId, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    // ---- TRAINER

    [Fact]
    public async Task Execute_ShouldPrescribeTheWorkoutToTheUserWhenATrainerCreatesIt()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().Be(TrainerId);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerWhoSendsNoOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, null, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerNamingThemselvesAsOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, TrainerId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerNamingATrainer()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, UserId, Id.New(), Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectATrainerNamingAnUnknownOwner()
    {
        // Arrange
        GivenNoUser(UserId);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, UserId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Theory]
    [InlineData(Role.TRAINER)]
    [InlineData(Role.ADMIN)]
    public async Task Execute_ShouldRejectATrainerNamingAnOwnerWhoIsNotAUser(Role ownerRole)
    {
        // Arrange
        var ownerId = Id.New();
        GivenStored(ownerId, ownerRole);

        // Act
        var act = () => AUseCase().Execute(TrainerId, Role.TRAINER, ownerId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedOperationException>();
        VerifyNothingSavedOrCommitted();
    }

    // ---- ADMIN

    [Fact]
    public async Task Execute_ShouldCreateTheWorkoutWithoutTrainerWhenAnAdminSendsNone()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(AdminId, Role.ADMIN, UserId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().BeNull();
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldAssignTheTrainerWhenAnAdminSendsOneWhoCanTrain()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenTrainerCheck(TrainerId, true);
        GivenSaveWorks();

        // Act
        var workout = await AUseCase().Execute(AdminId, Role.ADMIN, UserId, TrainerId, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        workout.UserId.Should().Be(UserId);
        workout.TrainerId.Should().Be(TrainerId);
        VerifySavedAndCommittedOnce(workout);
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminWhoSendsNoOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, null, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminNamingThemselvesAsOwner()
    {
        // Arrange & Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, AdminId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
        VerifyNoUserLookups();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminNamingAnUnknownOwner()
    {
        // Arrange
        GivenNoUser(UserId);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, UserId, null, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldRejectAnAdminNamingATrainerWhoCannotTrain()
    {
        // Arrange
        GivenStored(UserId, Role.USER);
        GivenTrainerCheck(TrainerId, false);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, UserId, TrainerId, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidWorkoutAssignmentException>();
        VerifyNothingSavedOrCommitted();
    }

    [Fact]
    public async Task Execute_ShouldPropagateTheDomainErrorWhenTheTrainerIsTheOwner()
    {
        // Arrange
        GivenStored(TrainerId, Role.TRAINER);
        GivenTrainerCheck(TrainerId, true);

        // Act
        var act = () => AUseCase().Execute(AdminId, Role.ADMIN, TrainerId, TrainerId, Name, null, WorkoutDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TrainerCannotBeWorkoutOwnerException>();
        VerifyNothingSavedOrCommitted();
    }
}

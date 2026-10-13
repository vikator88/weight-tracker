using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WorkoutTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 18, 30, 0, DateTimeKind.Utc);

    private static readonly SetPrescription FourByTen = new(4, new SetTarget.Reps(10));

    [Fact]
    public void Create_ShouldRejectATrainerWhoIsTheOwner()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var act = () => Workout.Create(userId, userId, WorkoutDate);

        // Assert
        act.Should().Throw<TrainerCannotBeWorkoutOwnerException>();
    }

    [Fact]
    public void Create_ShouldAcceptAWorkoutWithoutTrainer()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var workout = Workout.Create(userId, null, WorkoutDate);

        // Assert
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldAcceptATrainerDifferentFromTheOwner()
    {
        // Arrange
        var userId = Id.New();
        var trainerId = Id.New();

        // Act
        var workout = Workout.Create(userId, trainerId, WorkoutDate);

        // Assert
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().Be(trainerId);
    }

    [Fact]
    public void Create_ShouldMarkTheWorkoutAsNew()
    {
        // Arrange
        var userId = Id.New();

        // Act
        var workout = Workout.Create(userId, null, WorkoutDate);

        // Assert
        workout.IsNew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTruncateTheWorkoutDateToTheDay()
    {
        // Arrange & Act
        var workout = Workout.Create(Id.New(), null, WorkoutDate);

        // Assert
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
    }

    [Fact]
    public void AddExercise_ShouldRegisterTheExerciseWithItsEffortAndSets()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exerciseId = Id.New();
        var twoByEight = new SetPrescription(2, new SetTarget.Reps(8));

        // Act
        workout.AddExercise(exerciseId, Effort.HIGH, [FourByTen, twoByEight]);

        // Assert
        var entry = workout.Exercises.Should().ContainSingle().Subject;
        entry.ExerciseId.Should().Be(exerciseId);
        entry.Effort.Should().Be(Effort.HIGH);
        entry.Sets.Should().ContainInOrder(FourByTen, twoByEight);
        entry.IsNew.Should().BeTrue();
    }

    [Fact]
    public void AddExercise_ShouldAppendInInsertionOrder()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [FourByTen]);

        // Act
        workout.AddExercise(second, Effort.LOW, [new SetPrescription(5, new SetTarget.Reps(5))]);

        // Assert
        workout.Exercises.Select(entry => entry.ExerciseId).Should().Equal(first, second);
    }

    [Fact]
    public void AddExercise_ShouldRejectAnExerciseAlreadyInTheWorkout()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        var act = () => workout.AddExercise(exerciseId, Effort.HIGH, [new SetPrescription(3, new SetTarget.MaxReps())]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInWorkoutException>();
        var entry = workout.Exercises.Should().ContainSingle().Subject;
        entry.Effort.Should().Be(Effort.MID);
        entry.Sets.Should().Equal(FourByTen);
    }

    [Fact]
    public void AddExercise_ShouldRejectAnUndefinedEffort()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);

        // Act
        var act = () => workout.AddExercise(Id.New(), (Effort)0, [FourByTen]);

        // Assert
        act.Should().Throw<InvalidEffortException>();
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExercise_ShouldDropTheExercise()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(exerciseId, Effort.MID, [FourByTen]);

        // Act
        workout.RemoveExercise(exerciseId);

        // Assert
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExercise_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);

        // Act
        var act = () => workout.RemoveExercise(Id.New());

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheWorkoutAsNew()
    {
        // Arrange & Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, WorkoutDate, []);

        // Assert
        workout.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreOwnershipAndOrderedExercises()
    {
        // Arrange
        var id = Id.New();
        var userId = Id.New();
        var trainerId = Id.New();
        var sentadilla = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MIDHIGH, [new SetPrescription(5, new SetTarget.Reps(5))]);
        var plancha = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(40))]);

        // Act
        var workout = Workout.Rehydrate(id, userId, trainerId, WorkoutDate, [sentadilla, plancha]);

        // Assert
        workout.Id.Should().Be(id);
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().Be(trainerId);
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
        workout.Exercises.Should().Equal(sentadilla, plancha);
        workout.Exercises[1].Sets.Single().target.Should().Be(new SetTarget.Duration(40));
    }

    [Fact]
    public void Rehydrate_ShouldRejectTheSameExerciseTwice()
    {
        // Arrange
        var exerciseId = Id.New();
        var first = WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.MID, [FourByTen]);
        var second = WorkoutExercise.Rehydrate(Id.New(), exerciseId, Effort.HIGH, [FourByTen]);

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, WorkoutDate, [first, second]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInWorkoutException>();
    }

    [Fact]
    public void Exercises_ShouldNotExposeTheInternalCollectionForMutation()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        workout.AddExercise(Id.New(), Effort.MID, [FourByTen]);

        var snapshot = workout.Exercises;

        // Act
        workout.AddExercise(Id.New(), Effort.LOW, [new SetPrescription(2, new SetTarget.Reps(8))]);

        // Assert
        snapshot.Should().HaveCount(1);
        workout.Exercises.Should().HaveCount(2);
    }
}

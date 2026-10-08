using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Workouts;

public class WorkoutTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 1, 18, 30, 0, DateTimeKind.Utc);

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
    public void AddExercise_ShouldRegisterTheExerciseWithItsSets()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exercise = new Exercise("Press Banca", BodyParts.CHEST);

        // Act
        workout.AddExercise(exercise, [new SetPrescription(4, new SetTarget.Reps(10))]);

        // Assert
        workout.Exercises.Should().ContainKey(exercise);
        workout.Exercises[exercise].Should().HaveCount(1);
    }

    [Fact]
    public void AddExercise_ShouldMergeIntoAnEquivalentExerciseInstance()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exerciseId = Id.New();

        var stored = Exercise.Rehydrate(exerciseId, "Press Banca", null, BodyParts.CHEST);
        var equivalent = Exercise.Rehydrate(exerciseId, "Press Banca", null, BodyParts.CHEST);

        workout.AddExercise(stored, [new SetPrescription(4, new SetTarget.Reps(10))]);

        // Act
        workout.AddExercise(equivalent, [new SetPrescription(3, new SetTarget.MaxReps())]);

        // Assert
        workout.Exercises.Should().HaveCount(1);
        workout.Exercises[stored].Should().HaveCount(2);
    }

    [Fact]
    public void AddExercise_ShouldKeepDistinctExercisesApart()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);

        workout.AddExercise(new Exercise("Press Banca", BodyParts.CHEST), [new SetPrescription(4, new SetTarget.Reps(10))]);

        // Act
        workout.AddExercise(new Exercise("Sentadilla", BodyParts.LEGS), [new SetPrescription(5, new SetTarget.Reps(5))]);

        // Assert
        workout.Exercises.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveExercise_ShouldDropTheExercise()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exercise = new Exercise("Press Banca", BodyParts.CHEST);
        workout.AddExercise(exercise, [new SetPrescription(4, new SetTarget.Reps(10))]);

        // Act
        workout.RemoveExercise(exercise);

        // Assert
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExercise_ShouldRejectAnExerciseNotInTheWorkout()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);

        // Act
        var act = () => workout.RemoveExercise(new Exercise("Peso Muerto", BodyParts.BACK));

        // Assert
        act.Should().Throw<ExerciseNotInWorkoutException>();
    }

    [Fact]
    public void RemoveExercise_ShouldAcceptAnEquivalentExerciseInstance()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exerciseId = Id.New();
        workout.AddExercise(
            Exercise.Rehydrate(exerciseId, "Press Banca", null, BodyParts.CHEST),
            [new SetPrescription(4, new SetTarget.Reps(10))]);

        // Act
        workout.RemoveExercise(Exercise.Rehydrate(exerciseId, "Press Banca", null, BodyParts.CHEST));

        // Assert
        workout.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheWorkoutAsNew()
    {
        // Arrange & Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, WorkoutDate, new());

        // Assert
        workout.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreOwnershipAndPrescriptions()
    {
        // Arrange
        var id = Id.New();
        var userId = Id.New();
        var trainerId = Id.New();
        var exercise = Exercise.Rehydrate(Id.New(), "Plancha", null, BodyParts.BACK);

        // Act
        var workout = Workout.Rehydrate(id, userId, trainerId, WorkoutDate, new()
        {
            [exercise] = [new SetPrescription(3, new SetTarget.Duration(40))],
        });

        // Assert
        workout.Id.Should().Be(id);
        workout.UserId.Should().Be(userId);
        workout.TrainerId.Should().Be(trainerId);
        workout.WorkoutDate.Should().Be(WorkoutDate.Date);
        workout.Exercises[exercise].Single().target.Should().Be(new SetTarget.Duration(40));
    }

    [Fact]
    public void Exercises_ShouldNotExposeTheInternalCollectionForMutation()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var exercise = new Exercise("Press Banca", BodyParts.CHEST);
        workout.AddExercise(exercise, [new SetPrescription(4, new SetTarget.Reps(10))]);

        var snapshot = workout.Exercises;

        // Act
        workout.AddExercise(exercise, [new SetPrescription(2, new SetTarget.Reps(8))]);

        // Assert
        snapshot[exercise].Should().HaveCount(1);
        workout.Exercises[exercise].Should().HaveCount(2);
    }
}

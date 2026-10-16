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

    private static readonly SetPrescription ThreeByTen = new(3, new SetTarget.Reps(10));

    /// <param name="entryIds">Identifiers of the workout exercise entries, not of the catalog exercises.</param>
    private static Workout WorkoutWithExercises(out Id[] entryIds, int count = 3)
    {
        var workout = Workout.Create(Id.New(), null, WorkoutDate);

        for (var index = 0; index < count; index++)
            workout.AddExercise(Id.New(), Effort.MID, [FourByTen]);

        entryIds = workout.Exercises.Select(entry => entry.Id).ToArray();

        return workout;
    }

    private static Id EntryOf(Workout workout, Id exerciseId)
        => workout.Exercises.Single(entry => entry.ExerciseId == exerciseId).Id;

    private static Id ExerciseOf(Workout workout, Id entryId)
        => workout.Exercises.Single(entry => entry.Id == entryId).ExerciseId;

    private static SuperSeriesId? SuperSeriesOf(Workout workout, Id entryId)
        => workout.Exercises.Single(entry => entry.Id == entryId).SuperSeriesId;

    [Fact]
    public void CreateSuperSeries_ShouldGroupTheGivenExercisesAndLeaveTheOthersAlone()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var superSeriesId = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Assert
        SuperSeriesOf(workout, ids[0]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[2]).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldAssignADifferentIdToEachSuperSeriesOfTheWorkout()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids, 4);
        var first = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        var second = workout.CreateSuperSeries([ids[2], ids[3]]);

        // Assert
        second.Should().NotBe(first);
    }

    [Fact]
    public void CreateSuperSeries_ShouldAllowTheSameIdInDifferentWorkouts()
    {
        // Arrange
        var firstWorkout = WorkoutWithExercises(out var firstIds, 2);
        var secondWorkout = WorkoutWithExercises(out var secondIds, 2);

        // Act
        var firstSuperSeries = firstWorkout.CreateSuperSeries(firstIds);
        var secondSuperSeries = secondWorkout.CreateSuperSeries(secondIds);

        // Assert
        secondSuperSeries.Should().Be(firstSuperSeries);
    }

    [Fact]
    public void CreateSuperSeries_ShouldAcceptDifferentPrescriptionsWithTheSameTotalOfSets()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [ThreeByTen]);
        workout.AddExercise(second, Effort.MID, [new SetPrescription(2, new SetTarget.Reps(8)), new SetPrescription(1, new SetTarget.MaxReps())]);
        var firstEntry = EntryOf(workout, first);
        var secondEntry = EntryOf(workout, second);

        // Act
        var superSeriesId = workout.CreateSuperSeries([firstEntry, secondEntry]);

        // Assert
        SuperSeriesOf(workout, firstEntry).Should().Be(superSeriesId);
        SuperSeriesOf(workout, secondEntry).Should().Be(superSeriesId);
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectExercisesWithDifferentNumberOfSetsAndGroupNothing()
    {
        // Arrange
        var workout = Workout.Create(Id.New(), null, WorkoutDate);
        var first = Id.New();
        var second = Id.New();
        workout.AddExercise(first, Effort.MID, [ThreeByTen]);
        workout.AddExercise(second, Effort.MID, [FourByTen]);
        var firstEntry = EntryOf(workout, first);
        var secondEntry = EntryOf(workout, second);

        // Act
        var act = () => workout.CreateSuperSeries([firstEntry, secondEntry]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
        SuperSeriesOf(workout, firstEntry).Should().BeNull();
        SuperSeriesOf(workout, secondEntry).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectASingleExercise()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var act = () => workout.CreateSuperSeries([ids[0]]);

        // Assert
        act.Should().Throw<SuperSeriesRequiresTwoExercisesException>();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectTheSameExerciseTwice()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var act = () => workout.CreateSuperSeries([ids[0], ids[0]]);

        // Assert
        act.Should().Throw<DuplicateExerciseInSuperSeriesException>();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectAnExerciseNotInTheWorkoutAndGroupNothing()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);

        // Act
        var act = () => workout.CreateSuperSeries([ids[0], Id.New()]);

        // Assert
        act.Should().Throw<WorkoutExerciseNotInWorkoutException>();
        SuperSeriesOf(workout, ids[0]).Should().BeNull();
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectCatalogExerciseIdsAndGroupNothing()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var catalogIds = new[] { ExerciseOf(workout, ids[0]), ExerciseOf(workout, ids[1]) };

        // Act
        var act = () => workout.CreateSuperSeries(catalogIds);

        // Assert
        act.Should().Throw<WorkoutExerciseNotInWorkoutException>();
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public void CreateSuperSeries_ShouldRejectAnExerciseAlreadyInASuperSeries()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var existing = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        var act = () => workout.CreateSuperSeries([ids[1], ids[2]]);

        // Assert
        act.Should().Throw<ExerciseAlreadyInSuperSeriesException>();
        SuperSeriesOf(workout, ids[1]).Should().Be(existing);
        SuperSeriesOf(workout, ids[2]).Should().BeNull();
    }

    [Fact]
    public void BreakSuperSeries_ShouldUngroupTheExercisesAndKeepThemInTheWorkout()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var superSeriesId = workout.CreateSuperSeries([ids[0], ids[1]]);

        // Act
        workout.BreakSuperSeries(superSeriesId);

        // Assert
        workout.Exercises.Select(entry => entry.Id).Should().Equal(ids);
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == null);
    }

    [Fact]
    public void BreakSuperSeries_ShouldLeaveOtherSuperSeriesUntouched()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids, 4);
        var first = workout.CreateSuperSeries([ids[0], ids[1]]);
        var second = workout.CreateSuperSeries([ids[2], ids[3]]);

        // Act
        workout.BreakSuperSeries(first);

        // Assert
        SuperSeriesOf(workout, ids[0]).Should().BeNull();
        SuperSeriesOf(workout, ids[2]).Should().Be(second);
        SuperSeriesOf(workout, ids[3]).Should().Be(second);
    }

    [Fact]
    public void BreakSuperSeries_ShouldRejectAnUnknownSuperSeries()
    {
        // Arrange
        var workout = WorkoutWithExercises(out _);

        // Act
        var act = () => workout.BreakSuperSeries(SuperSeriesId.From(1));

        // Assert
        act.Should().Throw<SuperSeriesNotFoundException>();
    }

    [Fact]
    public void RemoveExercise_ShouldUngroupTheRemainingExerciseOfAPair()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids, 2);
        workout.CreateSuperSeries(ids);
        var removed = ExerciseOf(workout, ids[0]);

        // Act
        workout.RemoveExercise(removed);

        // Assert
        workout.Exercises.Should().ContainSingle().Which.SuperSeriesId.Should().BeNull();
    }

    [Fact]
    public void RemoveExercise_ShouldKeepTheRestOfALargerSuperSeriesGrouped()
    {
        // Arrange
        var workout = WorkoutWithExercises(out var ids);
        var superSeriesId = workout.CreateSuperSeries(ids);
        var removed = ExerciseOf(workout, ids[0]);

        // Act
        workout.RemoveExercise(removed);

        // Assert
        SuperSeriesOf(workout, ids[1]).Should().Be(superSeriesId);
        SuperSeriesOf(workout, ids[2]).Should().Be(superSeriesId);
    }

    [Fact]
    public void Rehydrate_ShouldRestoreStoredSuperSeries()
    {
        // Arrange
        var superSeriesId = SuperSeriesId.From(1);
        var first = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);
        var second = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);

        // Act
        var workout = Workout.Rehydrate(Id.New(), Id.New(), null, WorkoutDate, [first, second]);

        // Assert
        workout.Exercises.Should().OnlyContain(entry => entry.SuperSeriesId == superSeriesId);
    }

    [Fact]
    public void Rehydrate_ShouldRejectASuperSeriesWithASingleMember()
    {
        // Arrange
        var alone = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], SuperSeriesId.From(1));

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, WorkoutDate, [alone]);

        // Assert
        act.Should().Throw<SuperSeriesRequiresTwoExercisesException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectASuperSeriesWithDifferentNumberOfSets()
    {
        // Arrange
        var superSeriesId = SuperSeriesId.From(1);
        var first = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [ThreeByTen], superSeriesId);
        var second = WorkoutExercise.Rehydrate(Id.New(), Id.New(), Effort.MID, [FourByTen], superSeriesId);

        // Act
        var act = () => Workout.Rehydrate(Id.New(), Id.New(), null, WorkoutDate, [first, second]);

        // Assert
        act.Should().Throw<SuperSeriesSetCountMismatchException>();
    }
}

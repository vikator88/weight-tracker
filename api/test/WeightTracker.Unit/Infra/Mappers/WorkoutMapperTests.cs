using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;
using WeightTracker.Infra.Mappers;
using Xunit;

namespace WeightTracker.Unit.Infra.Mappers;

public class WorkoutMapperTests
{
    private static readonly DateTime WorkoutDate = new(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Exercise Sentadilla =
        Exercise.Rehydrate(Id.From(Guid.CreateVersion7()), "Sentadilla", null, BodyParts.LEGS);

    private static readonly Exercise Plancha =
        Exercise.Rehydrate(Id.From(Guid.CreateVersion7()), "Plancha", "https://video", BodyParts.BACK);

    private static IReadOnlyDictionary<Guid, ExerciseEntity> KnownExercises()
        => new[] { Sentadilla, Plancha }
            .Select(ExerciseMapper.MapToEntity)
            .ToDictionary(entity => entity.Id);

    private static Workout AFullyLoadedWorkout(Id? trainerId = null)
        => Workout.Rehydrate(
            Id.New(), Id.New(), trainerId, WorkoutDate,
            new()
            {
                [Sentadilla] =
                [
                    new SetPrescription(5, new SetTarget.Reps(5)),
                    new SetPrescription(2, new SetTarget.MaxReps()),
                ],
                [Plancha] = [new SetPrescription(3, new SetTarget.Duration(40))],
            });

    private static Workout RoundTrip(Workout workout)
        => WorkoutMapper.MapToDomain(WorkoutMapper.MapToEntity(workout), KnownExercises());

    [Fact]
    public void RoundTrip_ShouldPreserveIdentityAndOwnership()
    {
        // Arrange
        var trainerId = Id.New();
        var original = AFullyLoadedWorkout(trainerId);

        // Act
        var restored = RoundTrip(original);

        // Assert
        restored.Id.Should().Be(original.Id);
        restored.UserId.Should().Be(original.UserId);
        restored.TrainerId.Should().Be(trainerId);
        restored.WorkoutDate.Should().Be(original.WorkoutDate);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveANullTrainer()
    {
        // Arrange
        var workout = AFullyLoadedWorkout();

        // Act
        var restored = RoundTrip(workout);

        // Assert
        restored.TrainerId.Should().BeNull();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveEveryExerciseAndItsSets()
    {
        // Arrange & Act
        var restored = RoundTrip(AFullyLoadedWorkout());

        // Assert
        restored.Exercises.Should().HaveCount(2);
        restored.Exercises[Sentadilla].Should().HaveCount(2);
        restored.Exercises[Plancha].Should().HaveCount(1);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveSetOrdering()
    {
        // Arrange & Act
        var restored = RoundTrip(AFullyLoadedWorkout());

        // Assert
        restored.Exercises[Sentadilla].Should().ContainInOrder(
            new SetPrescription(5, new SetTarget.Reps(5)),
            new SetPrescription(2, new SetTarget.MaxReps()));
    }

    [Fact]
    public void RoundTrip_ShouldPreserveExerciseOrdering()
    {
        // Arrange & Act
        var restored = RoundTrip(AFullyLoadedWorkout());

        // Assert
        restored.Exercises.Keys.Should().ContainInOrder(Sentadilla, Plancha);
    }

    [Theory]
    [MemberData(nameof(EverySetTargetVariant))]
    public void RoundTrip_ShouldPreserveEverySetTargetVariant(SetTarget target)
    {
        // Arrange
        var workout = Workout.Rehydrate(
            Id.New(), Id.New(), null, WorkoutDate,
            new() { [Sentadilla] = [new SetPrescription(4, target)] });

        // Act
        var restored = RoundTrip(workout);

        // Assert
        restored.Exercises[Sentadilla].Single().target.Should().Be(target);
    }

    public static TheoryData<SetTarget> EverySetTargetVariant() =>
    [
        new SetTarget.Reps(10),
        new SetTarget.Duration(40),
        new SetTarget.MaxReps(),
    ];

    [Fact]
    public void MapToEntity_ShouldLeaveMaxRepsWithoutAValue()
    {
        // Arrange
        var workout = Workout.Rehydrate(
            Id.New(), Id.New(), null, WorkoutDate,
            new() { [Sentadilla] = [new SetPrescription(3, new SetTarget.MaxReps())] });

        // Act
        var set = WorkoutMapper.MapToEntity(workout).WorkoutExercises.Single().Sets.Single();

        // Assert
        set.TargetType.Should().Be("MaxReps");
        set.TargetValue.Should().BeNull();
    }

    [Fact]
    public void MapToEntity_ShouldNumberPositionsFromZero()
    {
        // Arrange & Act
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());

        // Assert
        entity.WorkoutExercises.Select(item => item.Position).Should().ContainInOrder(0, 1);
        entity.WorkoutExercises.First().Sets.Select(set => set.Position).Should().ContainInOrder(0, 1);
    }

    [Fact]
    public void MapToDomain_ShouldHonourStoredPositionsRatherThanRowOrder()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.WorkoutExercises.Reverse();
        foreach (var workoutExercise in entity.WorkoutExercises)
            workoutExercise.Sets.Reverse();

        // Act
        var restored = WorkoutMapper.MapToDomain(entity, KnownExercises());

        // Assert
        restored.Exercises.Keys.Should().ContainInOrder(Sentadilla, Plancha);
        restored.Exercises[Sentadilla].Should().ContainInOrder(
            new SetPrescription(5, new SetTarget.Reps(5)),
            new SetPrescription(2, new SetTarget.MaxReps()));
    }

    [Fact]
    public void MapToDomain_ShouldRejectAWorkoutReferencingAnUnknownExercise()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity, new Dictionary<Guid, ExerciseEntity>());

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectAnUnknownTargetType()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        entity.WorkoutExercises.First().Sets.First().TargetType = "Furlongs";

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity, KnownExercises());

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void MapToDomain_ShouldRejectARepsTargetWithoutAValue()
    {
        // Arrange
        var entity = WorkoutMapper.MapToEntity(AFullyLoadedWorkout());
        var set = entity.WorkoutExercises.First().Sets.First(item => item.TargetType == "Reps");
        set.TargetValue = null;

        // Act
        var act = () => WorkoutMapper.MapToDomain(entity, KnownExercises());

        // Assert
        act.Should().Throw<PersistenceMappingException>();
    }

    [Fact]
    public void RoundTrip_ShouldMarkTheRestoredWorkoutAsNotNew()
    {
        // Arrange
        var workout = AFullyLoadedWorkout();

        // Act
        var restored = RoundTrip(workout);

        // Assert
        restored.IsNew.Should().BeFalse();
    }
}

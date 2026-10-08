using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers persistence of the Workout aggregate across its three tables, including the
/// mutation path that no endpoint reaches yet.
/// </summary>
public class WorkoutRepositoryTests : IntegrationTest
{
    public WorkoutRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private static Exercise PressBanca()
        => Exercise.Rehydrate(Id.From(SeedData.PressBancaId), "Press Banca", null, BodyParts.CHEST);

    private static Exercise Sentadilla()
        => Exercise.Rehydrate(Id.From(SeedData.SentadillaId), "Sentadilla", null, BodyParts.LEGS);

    private Task<Workout?> Read(Id workoutId)
        => Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>().GetById(workoutId, CancellationToken.None));

    private Task<bool> Write(Func<IWorkoutRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<IWorkoutRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    [Fact]
    public async Task Save_ShouldPersistANewAggregateAcrossItsThreeTables()
    {
        // Arrange
        var workout = Workout.Create(
            Id.From(SeedData.UserId), Id.From(SeedData.TrainerId), new DateTime(2026, 11, 2));
        workout.AddExercise(PressBanca(),
        [
            new SetPrescription(4, new SetTarget.Reps(10)),
            new SetPrescription(1, new SetTarget.MaxReps()),
        ]);

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored.Should().NotBeNull();
        stored!.UserId.Should().Be(Id.From(SeedData.UserId));
        stored.TrainerId.Should().Be(Id.From(SeedData.TrainerId));
        stored.WorkoutDate.Should().Be(new DateTime(2026, 11, 2));
        stored.IsNew.Should().BeFalse();
        stored.Exercises[PressBanca()].Should().ContainInOrder(
            new SetPrescription(4, new SetTarget.Reps(10)),
            new SetPrescription(1, new SetTarget.MaxReps()));
    }

    [Fact]
    public async Task Save_ShouldPersistAWorkoutWithoutATrainer()
    {
        // Arrange
        var workout = Workout.Create(Id.From(SeedData.UserId), null, new DateTime(2026, 11, 3));
        workout.AddExercise(Sentadilla(), [new SetPrescription(5, new SetTarget.Duration(30))]);

        await Write(repository => repository.Save(workout, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored!.TrainerId.Should().BeNull();
        stored.Exercises[Sentadilla()].Single().target.Should().Be(new SetTarget.Duration(30));
    }

    [Fact]
    public async Task Save_ShouldReplaceThePrescriptionsOfAnExistingAggregate()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.RemoveExercise(PressBanca());
        stored.AddExercise(Sentadilla(), [new SetPrescription(3, new SetTarget.Reps(12))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var reloaded = await Read(Id.From(SeedData.OwnedWorkoutId));

        // Assert
        reloaded!.Exercises.Should().HaveCount(1);
        reloaded.Exercises.Should().NotContainKey(PressBanca());
        reloaded.Exercises[Sentadilla()].Single().Should().Be(new SetPrescription(3, new SetTarget.Reps(12)));
    }

    [Fact]
    public async Task Save_ShouldNotDuplicateTheWorkoutRowOnUpdate()
    {
        // Arrange
        var stored = await Read(Id.From(SeedData.OwnedWorkoutId));
        stored!.AddExercise(Sentadilla(), [new SetPrescription(2, new SetTarget.Reps(20))]);

        await Write(repository => repository.Save(stored, CancellationToken.None));

        // Act
        var all = await Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutRepository>().GetAll(CancellationToken.None));

        // Assert
        all.Should().HaveCount(3);
        all.Count(workout => workout.Id == Id.From(SeedData.OwnedWorkoutId)).Should().Be(1);
    }

    [Fact]
    public async Task GetById_ShouldReturnNullForAnUnknownWorkout()
    {
        // Arrange
        var unknownId = Id.New();

        // Act
        var workout = await Read(unknownId);

        // Assert
        workout.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByUserId_ShouldIgnoreWorkoutsWhereTheUserIsOnlyTheTrainer()
    {
        // Arrange & Act
        var owned = await Factory.WithScope(services => services
            .GetRequiredService<IWorkoutRepository>()
            .GetAllByUserId(Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        owned.Select(workout => workout.Id)
            .Should().BeEquivalentTo(new[] { Id.From(SeedData.TrainerOwnWorkoutId) });
    }

    [Fact]
    public async Task GetAllByUserOrTrainerId_ShouldIncludeBothRoles()
    {
        // Arrange & Act
        var visible = await Factory.WithScope(services => services
            .GetRequiredService<IWorkoutRepository>()
            .GetAllByUserOrTrainerId(Id.From(SeedData.TrainerId), CancellationToken.None));

        // Assert
        visible.Select(workout => workout.Id).Should().BeEquivalentTo(new[]
        {
            Id.From(SeedData.TrainedWorkoutId),
            Id.From(SeedData.TrainerOwnWorkoutId),
        });
    }
}

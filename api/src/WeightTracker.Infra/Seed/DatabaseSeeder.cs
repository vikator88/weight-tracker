using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Mappers;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Seed;

/// <summary>
/// Populates the development and integration test dataset.
/// </summary>
/// <remarks>
/// Never runs in production. Seed code is the one place allowed to reach the DbContext
/// directly instead of going through the repositories, because it needs to assign fixed
/// identifiers that the normal creation path deliberately generates. Domain objects are
/// still built through the aggregates, so the seeded data respects every invariant.
/// </remarks>
public class DatabaseSeeder
{
    private readonly WeightTrackerDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(WeightTrackerDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task Seed(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
            return;

        await SeedExercises(cancellationToken);
        await SeedUsers(cancellationToken);
        await SeedWorkouts(cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedExercises(CancellationToken cancellationToken)
    {
        var exercises = new[]
        {
            Exercise.Rehydrate(Id.From(SeedData.PressBancaId), "Press Banca", null, BodyParts.CHEST),
            Exercise.Rehydrate(Id.From(SeedData.SentadillaId), "Sentadilla", null, BodyParts.LEGS),
            Exercise.Rehydrate(Id.From(SeedData.PlanchaId), "Plancha", null, BodyParts.BACK),
        };

        await _context.Exercises.AddRangeAsync(exercises.Select(ExerciseMapper.MapToEntity), cancellationToken);
    }

    private async Task SeedUsers(CancellationToken cancellationToken)
    {
        var passwordHash = _passwordHasher.Hash(SeedData.Password);

        var users = new[]
        {
            BuildUser(SeedData.UserId, SeedData.UserEmail, "Ada", "Lovelace",
                new DateOnly(1990, 4, 12), Role.USER, passwordHash),
            BuildUser(SeedData.TrainerId, SeedData.TrainerEmail, "Grace", "Hopper",
                new DateOnly(1985, 12, 9), Role.TRAINER, passwordHash),
            BuildUser(SeedData.AdminId, SeedData.AdminEmail, "Alan", "Turing",
                new DateOnly(1980, 6, 23), Role.ADMIN, passwordHash),
        };

        foreach (var user in users)
        {
            // Email is a unique business identifier and is checked before persisting
            if (await _context.Users.AnyAsync(stored => stored.Email == user.Email.Value, cancellationToken))
                continue;

            await _context.Users.AddAsync(UserMapper.MapToEntity(user), cancellationToken);
        }
    }

    private async Task SeedWorkouts(CancellationToken cancellationToken)
    {
        var pressBanca = Exercise.Rehydrate(Id.From(SeedData.PressBancaId), "Press Banca", null, BodyParts.CHEST);
        var sentadilla = Exercise.Rehydrate(Id.From(SeedData.SentadillaId), "Sentadilla", null, BodyParts.LEGS);
        var plancha = Exercise.Rehydrate(Id.From(SeedData.PlanchaId), "Plancha", null, BodyParts.BACK);

        var ownedWorkout = Workout.Rehydrate(
            Id.From(SeedData.OwnedWorkoutId),
            Id.From(SeedData.UserId),
            null,
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new()
            {
                [pressBanca] = [new SetPrescription(4, new SetTarget.Reps(10))],
            });

        var trainedWorkout = Workout.Rehydrate(
            Id.From(SeedData.TrainedWorkoutId),
            Id.From(SeedData.UserId),
            Id.From(SeedData.TrainerId),
            new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc),
            new()
            {
                [sentadilla] = [new SetPrescription(5, new SetTarget.Reps(5))],
                [plancha] = [new SetPrescription(3, new SetTarget.Duration(40))],
            });

        var trainerOwnWorkout = Workout.Rehydrate(
            Id.From(SeedData.TrainerOwnWorkoutId),
            Id.From(SeedData.TrainerId),
            null,
            new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            new()
            {
                [pressBanca] = [new SetPrescription(3, new SetTarget.MaxReps())],
            });

        var workouts = new[] { ownedWorkout, trainedWorkout, trainerOwnWorkout };

        await _context.Workouts.AddRangeAsync(workouts.Select(WorkoutMapper.MapToEntity), cancellationToken);
    }

    private static User BuildUser(
        Guid id, string email, string name, string surname, DateOnly dateBirth, Role role, string passwordHash)
        => User.Rehydrate(
            Id.From(id), Email.From(email), name, surname, dateBirth, role, passwordHash, SeedData.CreatedAt);
}

using Microsoft.EntityFrameworkCore;
using WeightTracker.Infra.Entities;

namespace WeightTracker.Infra.Persistence;

/// <summary>
/// Maps the EF entities onto the schema owned by FluentMigrator.
/// </summary>
/// <remarks>
/// Every table and column name is declared explicitly so this configuration and the
/// migrations stay in lockstep. EF migrations are never generated for this context.
/// </remarks>
public class WeightTrackerDbContext : DbContext
{
    public WeightTrackerDbContext(DbContextOptions<WeightTrackerDbContext> options) : base(options)
    { }

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<ExerciseEntity> Exercises => Set<ExerciseEntity>();

    public DbSet<WorkoutEntity> Workouts => Set<WorkoutEntity>();

    public DbSet<WorkoutExerciseEntity> WorkoutExercises => Set<WorkoutExerciseEntity>();

    public DbSet<WorkoutExerciseSetEntity> WorkoutExerciseSets => Set<WorkoutExerciseSetEntity>();

    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
            entity.Property(user => user.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(user => user.Surname).HasColumnName("surname").HasMaxLength(100).IsRequired();
            entity.Property(user => user.DateBirth).HasColumnName("date_birth").HasColumnType("date");
            entity.Property(user => user.Role).HasColumnName("role");
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<ExerciseEntity>(entity =>
        {
            entity.ToTable("exercises");
            entity.HasKey(exercise => exercise.Id);
            entity.Property(exercise => exercise.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(exercise => exercise.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(exercise => exercise.VideoUrl).HasColumnName("video_url").HasMaxLength(500);
            entity.Property(exercise => exercise.BodyPart).HasColumnName("body_part");
        });

        modelBuilder.Entity<WorkoutEntity>(entity =>
        {
            entity.ToTable("workouts");
            entity.HasKey(workout => workout.Id);
            entity.Property(workout => workout.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(workout => workout.UserId).HasColumnName("user_id");
            entity.Property(workout => workout.TrainerId).HasColumnName("trainer_id");
            entity.Property(workout => workout.WorkoutDate).HasColumnName("workout_date").HasColumnType("date");

            // Declared without navigation properties: the User aggregate stays referenced by
            // identifier only, while EF still orders writes to satisfy the real constraints.
            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(workout => workout.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(workout => workout.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(workout => workout.WorkoutExercises)
                .WithOne()
                .HasForeignKey(workoutExercise => workoutExercise.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExerciseEntity>(entity =>
        {
            entity.ToTable("workout_exercises");
            entity.HasKey(workoutExercise => workoutExercise.Id);
            entity.Property(workoutExercise => workoutExercise.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(workoutExercise => workoutExercise.WorkoutId).HasColumnName("workout_id");
            entity.Property(workoutExercise => workoutExercise.ExerciseId).HasColumnName("exercise_id");
            entity.Property(workoutExercise => workoutExercise.Position).HasColumnName("position");

            entity.HasIndex(workoutExercise => new { workoutExercise.WorkoutId, workoutExercise.ExerciseId }).IsUnique();

            // Exercise is its own aggregate root: foreign key only, no navigation
            entity.HasOne<ExerciseEntity>()
                .WithMany()
                .HasForeignKey(workoutExercise => workoutExercise.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(workoutExercise => workoutExercise.Sets)
                .WithOne()
                .HasForeignKey(set => set.WorkoutExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExerciseSetEntity>(entity =>
        {
            entity.ToTable("workout_exercise_sets");
            entity.HasKey(set => set.Id);
            entity.Property(set => set.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(set => set.WorkoutExerciseId).HasColumnName("workout_exercise_id");
            entity.Property(set => set.Position).HasColumnName("position");
            entity.Property(set => set.Count).HasColumnName("count");
            entity.Property(set => set.TargetType).HasColumnName("target_type").HasMaxLength(20).IsRequired();
            entity.Property(set => set.TargetValue).HasColumnName("target_value");
        });

        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(token => token.UserId).HasColumnName("user_id");
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.Property(token => token.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
            entity.Property(token => token.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
            entity.Property(token => token.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
            entity.Property(token => token.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.HasIndex(token => token.TokenHash).IsUnique();

            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Rotation links a revoked token to its replacement. EF has to know about this
            // self reference, otherwise it can emit the revoking UPDATE before the INSERT
            // of the replacement row and trip the foreign key.
            entity.HasOne<RefreshTokenEntity>()
                .WithMany()
                .HasForeignKey(token => token.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

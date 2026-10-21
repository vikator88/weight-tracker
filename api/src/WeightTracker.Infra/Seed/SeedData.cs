namespace WeightTracker.Infra.Seed;

/// <summary>
/// Fixed identities and credentials of the seeded dataset.
/// </summary>
/// <remarks>
/// Identifiers are fixed rather than generated so the integration suite can assert on
/// them directly. The shared password is a development and test credential only.
/// </remarks>
public static class SeedData
{
    public const string Password = "Passw0rd!";

    public const string UserEmail = "user@weighttracker.test";
    public const string TrainerEmail = "trainer@weighttracker.test";
    public const string AdminEmail = "admin@weighttracker.test";

    public static readonly Guid UserId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid TrainerId = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid AdminId = new("00000000-0000-0000-0000-000000000003");

    public static readonly Guid PressBancaId = new("00000000-0000-0000-0000-000000000011");
    public static readonly Guid SentadillaId = new("00000000-0000-0000-0000-000000000012");
    public static readonly Guid PlanchaId = new("00000000-0000-0000-0000-000000000013");
    /// <summary>Referenced by no workout, so it can be deleted.</summary>
    public static readonly Guid CurlBicepsId = new("00000000-0000-0000-0000-000000000014");

    /// <summary>Owned by the user, no trainer.</summary>
    public static readonly Guid OwnedWorkoutId = new("00000000-0000-0000-0000-000000000021");

    /// <summary>Owned by the user, prescribed by the trainer.</summary>
    public static readonly Guid TrainedWorkoutId = new("00000000-0000-0000-0000-000000000022");

    /// <summary>Owned by the trainer themselves, no trainer assigned.</summary>
    public static readonly Guid TrainerOwnWorkoutId = new("00000000-0000-0000-0000-000000000023");

    /// <summary>Press Banca in the owned workout, effort HIGH.</summary>
    public static readonly Guid OwnedPressBancaEntryId = new("00000000-0000-0000-0000-000000000031");

    /// <summary>Sentadilla in the trained workout, first, effort MIDHIGH.</summary>
    public static readonly Guid TrainedSentadillaEntryId = new("00000000-0000-0000-0000-000000000032");

    /// <summary>Plancha in the trained workout, second, effort LOW.</summary>
    public static readonly Guid TrainedPlanchaEntryId = new("00000000-0000-0000-0000-000000000033");

    /// <summary>Press Banca in the trainer's own workout, effort MID.</summary>
    public static readonly Guid TrainerOwnPressBancaEntryId = new("00000000-0000-0000-0000-000000000034");

    /// <summary>Owned by the user, prescribed by the trainer. Two ungrouped entries of 3 sets, so they can be grouped.</summary>
    public static readonly Guid PrescribedPairWorkoutId = new("00000000-0000-0000-0000-000000000024");

    /// <summary>Owned by the user, prescribed by the trainer. Press Banca and Sentadilla in superseries 1, Plancha alone.</summary>
    public static readonly Guid PrescribedSuperSeriesWorkoutId = new("00000000-0000-0000-0000-000000000025");

    /// <summary>Owned by the user, no trainer. Two ungrouped entries of 4 sets, so they can be grouped.</summary>
    public static readonly Guid SelfCreatedPairWorkoutId = new("00000000-0000-0000-0000-000000000026");

    /// <summary>Owned by the user, no trainer. Press Banca and Plancha in superseries 1.</summary>
    public static readonly Guid SelfCreatedSuperSeriesWorkoutId = new("00000000-0000-0000-0000-000000000027");

    /// <summary>Press Banca in the prescribed pair workout, 3 x 10 reps.</summary>
    public static readonly Guid PrescribedPairPressBancaEntryId = new("00000000-0000-0000-0000-000000000035");

    /// <summary>Sentadilla in the prescribed pair workout, 3 x 8 reps.</summary>
    public static readonly Guid PrescribedPairSentadillaEntryId = new("00000000-0000-0000-0000-000000000036");

    /// <summary>Press Banca in the prescribed superseries workout, superseries 1.</summary>
    public static readonly Guid PrescribedSuperSeriesPressBancaEntryId = new("00000000-0000-0000-0000-000000000037");

    /// <summary>Sentadilla in the prescribed superseries workout, superseries 1.</summary>
    public static readonly Guid PrescribedSuperSeriesSentadillaEntryId = new("00000000-0000-0000-0000-000000000038");

    /// <summary>Plancha in the prescribed superseries workout, not grouped.</summary>
    public static readonly Guid PrescribedSuperSeriesPlanchaEntryId = new("00000000-0000-0000-0000-000000000039");

    /// <summary>Sentadilla in the self-created pair workout, 4 x 6 reps.</summary>
    public static readonly Guid SelfCreatedPairSentadillaEntryId = new("00000000-0000-0000-0000-000000000040");

    /// <summary>Plancha in the self-created pair workout, 4 x 45 s.</summary>
    public static readonly Guid SelfCreatedPairPlanchaEntryId = new("00000000-0000-0000-0000-000000000041");

    /// <summary>Press Banca in the self-created superseries workout, superseries 1.</summary>
    public static readonly Guid SelfCreatedSuperSeriesPressBancaEntryId = new("00000000-0000-0000-0000-000000000042");

    /// <summary>Plancha in the self-created superseries workout, superseries 1.</summary>
    public static readonly Guid SelfCreatedSuperSeriesPlanchaEntryId = new("00000000-0000-0000-0000-000000000043");

    /// <summary>Owned by the user, prescribed by the trainer, still a draft. Hidden from the user.</summary>
    public static readonly Guid PrescribedDraftWorkoutId = new("00000000-0000-0000-0000-000000000028");

    /// <summary>Owned by the user, prescribed by the trainer, already started. Locked for editing.</summary>
    public static readonly Guid PrescribedStartedWorkoutId = new("00000000-0000-0000-0000-000000000029");

    /// <summary>Press Banca in the prescribed draft workout, 3 x 10 reps.</summary>
    public static readonly Guid PrescribedDraftPressBancaEntryId = new("00000000-0000-0000-0000-000000000044");

    /// <summary>Press Banca in the prescribed started workout, 3 x 10 reps.</summary>
    public static readonly Guid PrescribedStartedPressBancaEntryId = new("00000000-0000-0000-0000-000000000045");

    /// <summary>Identifier of the superseries seeded in the superseries workouts.</summary>
    public const int SeededSuperSeries = 1;

    public static readonly DateTime CreatedAt = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
}

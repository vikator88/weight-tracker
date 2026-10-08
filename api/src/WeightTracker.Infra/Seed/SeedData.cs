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

    /// <summary>Owned by the user, no trainer.</summary>
    public static readonly Guid OwnedWorkoutId = new("00000000-0000-0000-0000-000000000021");

    /// <summary>Owned by the user, prescribed by the trainer.</summary>
    public static readonly Guid TrainedWorkoutId = new("00000000-0000-0000-0000-000000000022");

    /// <summary>Owned by the trainer themselves, no trainer assigned.</summary>
    public static readonly Guid TrainerOwnWorkoutId = new("00000000-0000-0000-0000-000000000023");

    public static readonly DateTime CreatedAt = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
}

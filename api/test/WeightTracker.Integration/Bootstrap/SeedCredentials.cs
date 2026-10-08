using WeightTracker.Infra.Seed;

namespace WeightTracker.Integration.Bootstrap;

/// <summary>
/// Thin alias over <see cref="SeedData"/> so the tests read closer to the plan.
/// </summary>
public static class SeedCredentials
{
    public const string Password = SeedData.Password;

    public const string User = SeedData.UserEmail;
    public const string Trainer = SeedData.TrainerEmail;
    public const string Admin = SeedData.AdminEmail;
}

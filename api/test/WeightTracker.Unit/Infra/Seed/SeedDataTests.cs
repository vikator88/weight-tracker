using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Infra.Seed;
using Xunit;

namespace WeightTracker.Unit.Infra.Seed;

/// <summary>
/// Guards the seeded dataset against domain rules it must satisfy.
/// </summary>
/// <remarks>
/// The seeder builds its users through the aggregate, so a seed constant that breaks a
/// value object fails at runtime on the first seeding rather than at compile time. These
/// tests move that failure forward to the build.
/// </remarks>
public class SeedDataTests
{
    [Fact]
    public void SeededPassword_ShouldSatisfyTheStrengthRules()
    {
        // Arrange & Act
        var password = Password.From(SeedData.Password);

        // Assert
        password.Value.Should().Be(
            SeedData.Password,
            "the seeded credential is hashed through Password.From, so a weak value breaks seeding");
    }

    [Theory]
    [InlineData("user")]
    [InlineData("trainer")]
    [InlineData("admin")]
    public void SeededEmails_ShouldBeWellFormed(string account)
    {
        // Arrange
        var seeded = account switch
        {
            "user" => SeedData.UserEmail,
            "trainer" => SeedData.TrainerEmail,
            _ => SeedData.AdminEmail,
        };

        // Act
        var email = Email.From(seeded);

        // Assert
        email.Value.Should().Be(seeded);
    }

    [Fact]
    public void SeededIdentifiers_ShouldBeNonEmpty()
    {
        // Arrange
        var seeded = new[]
        {
            SeedData.UserId, SeedData.TrainerId, SeedData.AdminId,
            SeedData.PressBancaId, SeedData.SentadillaId, SeedData.PlanchaId,
            SeedData.OwnedWorkoutId, SeedData.TrainedWorkoutId, SeedData.TrainerOwnWorkoutId,
        };

        // Act
        var act = () => seeded.Select(Id.From).ToArray();

        // Assert
        act.Should().NotThrow();
    }
}

using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using Xunit;

namespace WeightTracker.Unit.Domain.Users;

public class UserTests
{
    private static readonly DateOnly Birth = new(1988, 5, 12);
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldMarkTheUserAsNew()
    {
        // Arrange & Act
        var user = User.Create(
            Email.From("user@weighttracker.test"), "Ada", "Lovelace", Birth, Role.USER, "hash", CreatedAt);

        // Assert
        user.IsNew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateAnIdentifier()
    {
        // Arrange & Act
        var user = User.Create(
            Email.From("user@weighttracker.test"), "Ada", "Lovelace", Birth, Role.USER, "hash", CreatedAt);

        // Assert
        user.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldKeepEveryProvidedField()
    {
        // Arrange
        var email = Email.From("trainer@weighttracker.test");

        // Act
        var user = User.Create(email, "Ada", "Lovelace", Birth, Role.TRAINER, "hashed-password", CreatedAt);

        // Assert
        user.Email.Should().Be(email);
        user.Name.Should().Be("Ada");
        user.Surname.Should().Be("Lovelace");
        user.DateBirth.Should().Be(Birth);
        user.Role.Should().Be(Role.TRAINER);
        user.PasswordHash.Should().Be("hashed-password");
        user.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheUserAsNew()
    {
        // Arrange & Act
        var user = User.Rehydrate(
            Id.New(), Email.From("user@weighttracker.test"), "Ada", "Lovelace", Birth, Role.USER, "hash", CreatedAt);

        // Assert
        user.IsNew.Should().BeFalse();
    }

    [Fact]
    public void Rehydrate_ShouldRestoreEveryField()
    {
        // Arrange
        var id = Id.New();
        var email = Email.From("admin@weighttracker.test");

        // Act
        var user = User.Rehydrate(id, email, "Grace", "Hopper", Birth, Role.ADMIN, "stored-hash", CreatedAt);

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.Name.Should().Be("Grace");
        user.Surname.Should().Be("Hopper");
        user.DateBirth.Should().Be(Birth);
        user.Role.Should().Be(Role.ADMIN);
        user.PasswordHash.Should().Be("stored-hash");
        user.CreatedAt.Should().Be(CreatedAt);
    }

    [Theory]
    [InlineData(Role.TRAINER, true)]
    [InlineData(Role.ADMIN, true)]
    [InlineData(Role.USER, false)]
    public void CanTrain_ShouldOnlyAllowTrainersAndAdmins(Role role, bool expected)
    {
        // Arrange
        var user = User.Create(
            Email.From("user@weighttracker.test"), "Ada", "Lovelace", Birth, role, "hash", CreatedAt);

        // Act
        var canTrain = user.CanTrain();

        // Assert
        canTrain.Should().Be(expected);
    }
}

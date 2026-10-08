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
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.USER, PasswordHash.From("hash"), CreatedAt);

        // Assert
        user.IsNew.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateAnIdentifier()
    {
        // Arrange & Act
        var user = User.Create(
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.USER, PasswordHash.From("hash"), CreatedAt);

        // Assert
        user.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldKeepEveryProvidedField()
    {
        // Arrange
        var email = Email.From("trainer@weighttracker.test");

        // Act
        var user = User.Create(
            email, PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, Role.TRAINER, PasswordHash.From("hashed-password"), CreatedAt);

        // Assert
        user.Email.Should().Be(email);
        user.Name.Should().Be(PersonName.From("Ada"));
        user.Surname.Should().Be(PersonName.From("Lovelace"));
        user.DateBirth.Should().Be(Birth);
        user.Role.Should().Be(Role.TRAINER);
        user.PasswordHash.Should().Be(PasswordHash.From("hashed-password"));
        user.CreatedAt.Should().Be(CreatedAt);
    }

    [Fact]
    public void Rehydrate_ShouldNotMarkTheUserAsNew()
    {
        // Arrange & Act
        var user = User.Rehydrate(
            Id.New(), Email.From("user@weighttracker.test"), PersonName.From("Ada"),
            PersonName.From("Lovelace"), Birth, Role.USER, PasswordHash.From("hash"), CreatedAt);

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
        var user = User.Rehydrate(
            id, email, PersonName.From("Grace"), PersonName.From("Hopper"),
            Birth, Role.ADMIN, PasswordHash.From("stored-hash"), CreatedAt);

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.Name.Should().Be(PersonName.From("Grace"));
        user.Surname.Should().Be(PersonName.From("Hopper"));
        user.DateBirth.Should().Be(Birth);
        user.Role.Should().Be(Role.ADMIN);
        user.PasswordHash.Should().Be(PasswordHash.From("stored-hash"));
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
            Email.From("user@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
            Birth, role, PasswordHash.From("hash"), CreatedAt);

        // Act
        var canTrain = user.CanTrain();

        // Assert
        canTrain.Should().Be(expected);
    }
}

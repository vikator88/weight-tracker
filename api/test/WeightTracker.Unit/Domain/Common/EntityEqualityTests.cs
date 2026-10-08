using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Domain.Common;

/// <summary>
/// Entities are used as dictionary keys inside aggregates, so identity equality
/// must be value based. Reference equality would silently duplicate entries when an
/// entity is rehydrated from persistence.
/// </summary>
public class EntityEqualityTests
{
    [Fact]
    public void SameTypeAndSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Id.New();

        var first = Exercise.Rehydrate(id, "Press Banca", null, BodyParts.CHEST);

        // Act
        var second = Exercise.Rehydrate(id, "Different Name", "https://video", BodyParts.LEGS);

        // Assert
        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void SameTypeAndSameId_ShouldShareHashCode()
    {
        // Arrange
        var id = Id.New();

        var first = Exercise.Rehydrate(id, "Press Banca", null, BodyParts.CHEST);

        // Act
        var second = Exercise.Rehydrate(id, "Press Banca", null, BodyParts.CHEST);

        // Assert
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void SameTypeAndDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var first = Exercise.Rehydrate(Id.New(), "Press Banca", null, BodyParts.CHEST);

        // Act
        var second = Exercise.Rehydrate(Id.New(), "Press Banca", null, BodyParts.CHEST);

        // Assert
        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void DifferentTypesWithSameId_ShouldNotBeEqual()
    {
        // Arrange
        var id = Id.New();

        Entity exercise = Exercise.Rehydrate(id, "Press Banca", null, BodyParts.CHEST);

        // Act
        Entity workout = Workout.Rehydrate(id, Id.New(), null, new DateTime(2026, 10, 1), new());

        // Assert
        exercise.Should().NotBe(workout);
    }

    [Fact]
    public void Entity_ShouldNotBeEqualToNull()
    {
        // Arrange & Act
        var exercise = Exercise.Rehydrate(Id.New(), "Press Banca", null, BodyParts.CHEST);

        // Assert
        exercise.Equals(null).Should().BeFalse();
        (exercise == null).Should().BeFalse();
        (exercise != null).Should().BeTrue();
    }

    [Fact]
    public void ExerciseRehydratedTwice_ShouldResolveToTheSameDictionaryKey()
    {
        // Arrange
        var id = Id.New();
        var dictionary = new Dictionary<Exercise, int>
        {
            [Exercise.Rehydrate(id, "Press Banca", null, BodyParts.CHEST)] = 1,
        };

        // Act
        var equivalent = Exercise.Rehydrate(id, "Press Banca", null, BodyParts.CHEST);

        // Assert
        dictionary.ContainsKey(equivalent).Should().BeTrue();
    }
}

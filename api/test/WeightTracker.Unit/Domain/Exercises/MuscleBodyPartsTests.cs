using FluentAssertions;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

public class MuscleBodyPartsTests
{
    private static readonly IReadOnlyDictionary<Muscles, BodyParts> Expected = new Dictionary<Muscles, BodyParts>
    {
        [Muscles.ABDOMINALS] = BodyParts.CORE,
        [Muscles.ABDUCTORS] = BodyParts.LEGS,
        [Muscles.ADDUCTORS] = BodyParts.LEGS,
        [Muscles.BICEPS] = BodyParts.ARMS,
        [Muscles.CALVES] = BodyParts.LEGS,
        [Muscles.CHEST] = BodyParts.CHEST,
        [Muscles.FOREARMS] = BodyParts.ARMS,
        [Muscles.GLUTES] = BodyParts.LEGS,
        [Muscles.HAMSTRINGS] = BodyParts.LEGS,
        [Muscles.LATS] = BodyParts.BACK,
        [Muscles.LOWER_BACK] = BodyParts.BACK,
        [Muscles.MIDDLE_BACK] = BodyParts.BACK,
        [Muscles.NECK] = BodyParts.NECK,
        [Muscles.QUADRICEPS] = BodyParts.LEGS,
        [Muscles.SHOULDERS] = BodyParts.SHOULDERS,
        [Muscles.TRAPS] = BodyParts.BACK,
        [Muscles.TRICEPS] = BodyParts.ARMS,
    };

    public static TheoryData<Muscles> EveryMuscle => new(Enum.GetValues<Muscles>());

    [Theory]
    [MemberData(nameof(EveryMuscle))]
    public void BodyPartOf_ShouldMapEveryMuscleToItsBodyPart(Muscles muscle)
    {
        // Arrange
        // A muscle missing from the table throws KeyNotFoundException and fails the test
        var expected = Expected[muscle];

        // Act
        var bodyPart = MuscleBodyParts.BodyPartOf(muscle);

        // Assert
        bodyPart.Should().Be(expected);
    }

    [Fact]
    public void BodyPartOf_ShouldRejectAnUndefinedMuscle()
    {
        // Arrange & Act
        var act = () => MuscleBodyParts.BodyPartOf((Muscles)99);

        // Assert
        act.Should().Throw<InvalidMuscleException>();
    }
}

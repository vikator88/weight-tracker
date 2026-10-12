using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

public class ExerciseTests
{
    private static readonly ExerciseName PressBanca = ExerciseName.From("Press Banca");
    private static readonly Url Video = Url.From("https://videos.example.com/press-banca");

    [Fact]
    public void Create_ShouldGenerateAnIdAndMarkTheExerciseAsNew()
    {
        // Arrange & Act
        var exercise = Exercise.Create(PressBanca, Video, BodyParts.CHEST);

        // Assert
        exercise.IsNew.Should().BeTrue();
        exercise.Id.Value.Should().NotBe(Guid.Empty);
        exercise.Name.Should().Be(PressBanca);
        exercise.VideoUrl.Should().Be(Video);
        exercise.BodyPart.Should().Be(BodyParts.CHEST);
    }

    [Fact]
    public void Create_ShouldGenerateADifferentIdEachTime()
    {
        // Arrange
        var first = Exercise.Create(PressBanca, null, BodyParts.CHEST);

        // Act
        var second = Exercise.Create(PressBanca, null, BodyParts.CHEST);

        // Assert
        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public void Create_ShouldAcceptAMissingVideoUrl()
    {
        // Arrange & Act
        var exercise = Exercise.Create(PressBanca, null, BodyParts.CHEST);

        // Assert
        exercise.VideoUrl.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldRejectAnUndefinedBodyPart()
    {
        // Arrange & Act
        var act = () => Exercise.Create(PressBanca, null, (BodyParts)99);

        // Assert
        act.Should().Throw<InvalidBodyPartException>();
    }

    [Fact]
    public void Rehydrate_ShouldKeepTheIdAndMarkTheExerciseAsNotNew()
    {
        // Arrange
        var id = Id.New();

        // Act
        var exercise = Exercise.Rehydrate(id, PressBanca, Video, BodyParts.CHEST);

        // Assert
        exercise.IsNew.Should().BeFalse();
        exercise.Id.Should().Be(id);
        exercise.Name.Should().Be(PressBanca);
        exercise.VideoUrl.Should().Be(Video);
        exercise.BodyPart.Should().Be(BodyParts.CHEST);
    }

    [Fact]
    public void Update_ShouldReplaceEveryAttributeAndKeepTheIdentity()
    {
        // Arrange
        var id = Id.New();
        var exercise = Exercise.Rehydrate(id, PressBanca, Video, BodyParts.CHEST);
        var newName = ExerciseName.From("Sentadilla");
        var newVideo = Url.From("https://videos.example.com/sentadilla");

        // Act
        exercise.Update(newName, newVideo, BodyParts.LEGS);

        // Assert
        exercise.Id.Should().Be(id);
        exercise.IsNew.Should().BeFalse();
        exercise.Name.Should().Be(newName);
        exercise.VideoUrl.Should().Be(newVideo);
        exercise.BodyPart.Should().Be(BodyParts.LEGS);
    }

    [Fact]
    public void Update_ShouldClearTheVideoUrlWhenNoneIsGiven()
    {
        // Arrange
        var exercise = Exercise.Rehydrate(Id.New(), PressBanca, Video, BodyParts.CHEST);

        // Act
        exercise.Update(PressBanca, null, BodyParts.CHEST);

        // Assert
        exercise.VideoUrl.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldRejectAnUndefinedBodyPartAndLeaveTheExerciseUntouched()
    {
        // Arrange
        var exercise = Exercise.Rehydrate(Id.New(), PressBanca, Video, BodyParts.CHEST);

        // Act
        var act = () => exercise.Update(ExerciseName.From("Sentadilla"), null, (BodyParts)99);

        // Assert
        act.Should().Throw<InvalidBodyPartException>();
        exercise.Name.Should().Be(PressBanca);
        exercise.VideoUrl.Should().Be(Video);
        exercise.BodyPart.Should().Be(BodyParts.CHEST);
    }

    [Fact]
    public void BodyPartSetter_ShouldAcceptADefinedValue()
    {
        // Arrange
        var exercise = Exercise.Create(PressBanca, null, BodyParts.CHEST);

        // Act
        exercise.BodyPart = BodyParts.ARMS;

        // Assert
        exercise.BodyPart.Should().Be(BodyParts.ARMS);
    }

    [Fact]
    public void BodyPartSetter_ShouldRejectAnUndefinedValue()
    {
        // Arrange
        var exercise = Exercise.Create(PressBanca, null, BodyParts.CHEST);

        // Act
        var act = () => exercise.BodyPart = (BodyParts)0;

        // Assert
        act.Should().Throw<InvalidBodyPartException>();
        exercise.BodyPart.Should().Be(BodyParts.CHEST);
    }
}

using FluentAssertions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using Xunit;

namespace WeightTracker.Unit.Domain.Exercises;

/// <summary>
/// Exercises are a read-only catalog, so restoring them is the only behaviour to cover.
/// </summary>
public class ExerciseTests
{
    private static readonly FreeExerciseDbId BenchPress = FreeExerciseDbId.From("Barbell_Bench_Press_-_Medium_Grip");
    private static readonly Url Video = Url.From("https://videos.example.com/press-banca");
    private static readonly LanguageCode Spanish = LanguageCode.From("es");
    private static readonly LanguageCode English = LanguageCode.From("en");
    private static readonly ExerciseName PressDeBanca = ExerciseName.From("Press de banca");
    private static readonly ExerciseName BenchPressName = ExerciseName.From("Barbell Bench Press - Medium Grip");

    [Fact]
    public void Rehydrate_ShouldRestoreEveryFieldAndTranslationAndMarkTheExerciseAsNotNew()
    {
        // Arrange
        var id = Id.New();
        var spanish = ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca);
        var english = ExerciseTranslation.Rehydrate(Id.New(), English, BenchPressName);

        // Act
        var exercise = Exercise.Rehydrate(id, BenchPress, Video, Muscles.CHEST, ExerciseTypes.STRENGTH, [spanish, english]);

        // Assert
        exercise.IsNew.Should().BeFalse();
        exercise.Id.Should().Be(id);
        exercise.FreeExerciseDbId.Should().Be(BenchPress);
        exercise.VideoUrl.Should().Be(Video);
        exercise.Muscle.Should().Be(Muscles.CHEST);
        exercise.Translations.Should().Equal(spanish, english);
        exercise.Translations.Should().OnlyContain(translation => translation.IsNew == false);
    }

    [Fact]
    public void Rehydrate_ShouldAcceptAMissingVideoUrlFreeExerciseDbIdAndTranslations()
    {
        // Arrange & Act
        var exercise = Exercise.Rehydrate(Id.New(), null, null, Muscles.NECK, ExerciseTypes.STRENGTH, []);

        // Assert
        exercise.VideoUrl.Should().BeNull();
        exercise.FreeExerciseDbId.Should().BeNull();
        exercise.Translations.Should().BeEmpty();
    }

    [Fact]
    public void Rehydrate_ShouldRejectTwoTranslationsForTheSameLanguage()
    {
        // Arrange
        var first = ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca);
        var second = ExerciseTranslation.Rehydrate(Id.New(), Spanish, ExerciseName.From("Press banca"));

        // Act
        var act = () => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [first, second]);

        // Assert
        act.Should().Throw<DuplicateExerciseTranslationException>();
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnUndefinedMuscle()
    {
        // Arrange
        var undefined = (Muscles)99;

        // Act
        var act = () => Exercise.Rehydrate(Id.New(), null, null, undefined, ExerciseTypes.STRENGTH, []);

        // Assert
        act.Should().Throw<InvalidMuscleException>();
    }

    [Theory]
    [InlineData(ExerciseTypes.STRENGTH)]
    [InlineData(ExerciseTypes.CARDIO)]
    [InlineData(ExerciseTypes.STRETCHING)]
    public void Rehydrate_ShouldKeepTheGivenType(ExerciseTypes type)
    {
        // Arrange
        var id = Id.New();

        // Act
        var exercise = Exercise.Rehydrate(id, null, null, Muscles.CHEST, type, []);

        // Assert
        exercise.Type.Should().Be(type);
    }

    [Fact]
    public void Rehydrate_ShouldRejectAnUndefinedType()
    {
        // Arrange
        var undefined = (ExerciseTypes)99;

        // Act
        var act = () => Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, undefined, []);

        // Assert
        act.Should().Throw<InvalidExerciseTypeException>();
    }

    [Fact]
    public void Type_ShouldNotChangeTheMuscleOrTheBodyPart()
    {
        // Arrange
        var exercise = Exercise.Rehydrate(Id.New(), null, null, Muscles.QUADRICEPS, ExerciseTypes.CARDIO, []);

        // Act
        var bodyPart = exercise.BodyPart;

        // Assert
        exercise.Muscle.Should().Be(Muscles.QUADRICEPS);
        bodyPart.Should().Be(BodyParts.LEGS);
    }

    [Theory]
    [InlineData(Muscles.CHEST, BodyParts.CHEST)]
    [InlineData(Muscles.HAMSTRINGS, BodyParts.LEGS)]
    [InlineData(Muscles.ABDOMINALS, BodyParts.CORE)]
    public void BodyPart_ShouldBeDerivedFromTheStoredMuscle(Muscles muscle, BodyParts expected)
    {
        // Arrange
        var exercise = Exercise.Rehydrate(Id.New(), null, null, muscle, ExerciseTypes.STRENGTH, []);

        // Act
        var bodyPart = exercise.BodyPart;

        // Assert
        bodyPart.Should().Be(expected);
    }

    [Fact]
    public void Translations_ShouldBeASnapshotThatCannotChangeTheAggregate()
    {
        // Arrange
        var spanish = ExerciseTranslation.Rehydrate(Id.New(), Spanish, PressDeBanca);
        var exercise = Exercise.Rehydrate(Id.New(), null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, [spanish]);

        // Act
        ((List<ExerciseTranslation>)exercise.Translations).Clear();

        // Assert
        exercise.Translations.Should().Equal(spanish);
    }
}

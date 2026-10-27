using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// The repository only reads the catalog, so these cases cover the loads and their translations.
/// </summary>
public class ExerciseRepositoryTests : IntegrationTest
{
    public ExerciseRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private Task<Exercise?> Load(Id id)
        => Factory.WithScope(services => services.GetRequiredService<IExerciseRepository>().GetById(id, CancellationToken.None));

    [Fact]
    public async Task GetById_ShouldLoadTheSeededMuscleFreeExerciseDbIdAndTranslations()
    {
        // Arrange
        var id = Id.From(SeedData.SentadillaId);

        // Act
        var exercise = await Load(id);

        // Assert
        exercise!.Muscle.Should().Be(Muscles.QUADRICEPS);
        exercise.BodyPart.Should().Be(BodyParts.LEGS);
        exercise.FreeExerciseDbId!.Value.Should().Be(SeedData.SentadillaFreeExerciseDbId);
        exercise.Translations.Select(translation => (translation.Id.Value, translation.LanguageCode.Value, translation.Name.Value))
            .Should().BeEquivalentTo([
                (SeedData.SentadillaEsTranslationId, "es", "Sentadilla con barra"),
                (SeedData.SentadillaEnTranslationId, "en", "Barbell Squat"),
            ]);
    }

    [Fact]
    public async Task GetAllByIds_ShouldLoadTheTranslationsOfEveryExercise()
    {
        // Arrange
        var requested = new[] { Id.From(SeedData.PressBancaId), Id.From(SeedData.PlanchaId) };

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Should().OnlyContain(exercise => exercise.Translations.Count == 2);
    }

    [Fact]
    public async Task GetAllByIds_ShouldReturnOnlyTheRequestedExercises()
    {
        // Arrange
        var requested = new[] { Id.From(SeedData.PressBancaId), Id.From(SeedData.PlanchaId) };

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Select(exercise => exercise.Id).Should().BeEquivalentTo(requested);
    }

    [Fact]
    public async Task GetAllByIds_ShouldIgnoreUnknownIds()
    {
        // Arrange
        var requested = new[] { Id.From(SeedData.SentadillaId), Id.New() };

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Should().ContainSingle().Which.Id.Should().Be(Id.From(SeedData.SentadillaId));
    }

    [Fact]
    public async Task GetAllByIds_ShouldReturnNothingForNoIds()
    {
        // Arrange
        var requested = Array.Empty<Id>();

        // Act
        var exercises = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetAllByIds(requested, CancellationToken.None));

        // Assert
        exercises.Should().BeEmpty();
    }
}

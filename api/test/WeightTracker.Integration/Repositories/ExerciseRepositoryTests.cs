using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

public class ExerciseRepositoryTests : IntegrationTest
{
    public ExerciseRepositoryTests(ApiFactory factory) : base(factory)
    { }

    [Fact]
    public async Task Delete_ShouldRemoveTheExerciseOnceTheUnitOfWorkCommits()
    {
        // Arrange
        var id = Id.From(SeedData.CurlBicepsId);

        await Factory.WithScope(async services =>
        {
            var repository = services.GetRequiredService<IExerciseRepository>();
            var exercise = await repository.GetById(id, CancellationToken.None);

            await repository.Delete(exercise!, CancellationToken.None);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

        // Act
        var stored = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetById(id, CancellationToken.None));

        // Assert
        stored.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldNotRemoveAnythingUntilTheUnitOfWorkCommits()
    {
        // Arrange
        var id = Id.From(SeedData.CurlBicepsId);

        await Factory.WithScope(async services =>
        {
            var repository = services.GetRequiredService<IExerciseRepository>();
            var exercise = await repository.GetById(id, CancellationToken.None);

            await repository.Delete(exercise!, CancellationToken.None);

            return true;
        });

        // Act
        var stored = await Factory.WithScope(services =>
            services.GetRequiredService<IExerciseRepository>().GetById(id, CancellationToken.None));

        // Assert
        stored.Should().NotBeNull();
    }
}

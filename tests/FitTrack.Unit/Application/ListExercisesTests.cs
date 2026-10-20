using FitTrack.Application.Dtos;
using FitTrack.Application.UseCases;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class ListExercisesTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsTheCatalog()
    {
        var query = new InMemoryExerciseQueries();
        query.Seed(new ExerciseDto(Guid.NewGuid(), "Bench Press", Guid.NewGuid(), "Chest", IsActive: true));
        query.Seed(new ExerciseDto(Guid.NewGuid(), "Deadlift", Guid.NewGuid(), "Back", IsActive: false));

        var useCase = new ListExercises(query);

        var exercises = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, exercises.Count);
    }

    [Fact]
    public async Task ExecuteAsync_PagedFiltersBySearchAndMuscleGroup()
    {
        var chest = Guid.NewGuid();
        var back = Guid.NewGuid();
        var query = new InMemoryExerciseQueries();
        query.Seed(new ExerciseDto(Guid.NewGuid(), "Supino reto", chest, "Peito", IsActive: true));
        query.Seed(new ExerciseDto(Guid.NewGuid(), "Crucifixo", chest, "Peito", IsActive: true));
        query.Seed(new ExerciseDto(Guid.NewGuid(), "Remada curvada", back, "Costas", IsActive: true));

        var useCase = new ListExercises(query);

        var searched = await useCase.ExecuteAsync(ListQuery.Paged("costas", 1, 10), null, CancellationToken.None);
        var grouped = await useCase.ExecuteAsync(ListQuery.Paged(null, 1, 10), chest, CancellationToken.None);

        Assert.Equal(1, searched.TotalCount);
        Assert.Equal("Remada curvada", Assert.Single(searched.Items).Name);
        Assert.Equal(2, grouped.TotalCount);
        Assert.All(grouped.Items, exercise => Assert.Equal(chest, exercise.MuscleGroupId));
    }

    [Fact]
    public async Task ExecuteAsync_PagedSplitsTheCatalogIntoSizedPages()
    {
        var group = Guid.NewGuid();
        var query = new InMemoryExerciseQueries();

        for (var index = 1; index <= 5; index++)
        {
            query.Seed(new ExerciseDto(Guid.NewGuid(), $"Exercício {index:00}", group, "Peito", IsActive: true));
        }

        var useCase = new ListExercises(query);

        var page = await useCase.ExecuteAsync(ListQuery.Paged(null, 2, 2), null, CancellationToken.None);

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);
        Assert.Equal("Exercício 03", page.Items[0].Name);
        Assert.Equal("Exercício 04", page.Items[1].Name);
    }
}

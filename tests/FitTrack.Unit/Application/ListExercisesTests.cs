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
}

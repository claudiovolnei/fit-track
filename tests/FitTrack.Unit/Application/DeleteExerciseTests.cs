using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class DeleteExerciseTests
{
    private const string Name = "Bench Press";

    private readonly InMemoryExerciseRepository _exercises = new();
    private readonly InMemoryCatalogUsage _usage = new();
    private readonly Exercise _exercise;

    public DeleteExerciseTests()
    {
        _exercise = Exercise.Create(ExerciseId.New(), new ExerciseName(Name), MuscleGroupId.New());
        _exercises.SaveAsync(_exercise, CancellationToken.None).GetAwaiter().GetResult();
    }

    private DeleteExercise UseCase(ICurrentUser currentUser) => new(currentUser, _exercises, _usage);

    [Fact]
    public async Task ExecuteAsync_UnreferencedExercise_IsRemoved()
    {
        var useCase = UseCase(StubCurrentUser.PersonalTrainer(UserId.New()));

        await useCase.ExecuteAsync(new DeleteExerciseCommand(_exercise.Id.Value), CancellationToken.None);

        Assert.Null(await _exercises.GetByIdAsync(_exercise.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ReferencedExercise_IsRefusedAndKept()
    {
        _usage.ExerciseReferenced = true;
        var useCase = UseCase(StubCurrentUser.PersonalTrainer(UserId.New()));

        var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.ExecuteAsync(new DeleteExerciseCommand(_exercise.Id.Value), CancellationToken.None));

        Assert.Equal("exercise.in_use", conflict.Code);
        Assert.NotNull(await _exercises.GetByIdAsync(_exercise.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownExercise_Fails()
    {
        var useCase = UseCase(StubCurrentUser.PersonalTrainer(UserId.New()));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(new DeleteExerciseCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_AsAthlete_Fails()
    {
        var useCase = UseCase(StubCurrentUser.Athlete(UserId.New()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new DeleteExerciseCommand(_exercise.Id.Value), CancellationToken.None));
    }
}

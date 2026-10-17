using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class CreateExerciseTests
{
    private readonly InMemoryExerciseRepository _exercises = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();

    public CreateExerciseTests() =>
        _muscleGroups
            .SaveAsync(MuscleGroup.Create(_chest, new MuscleGroupName("Chest")), CancellationToken.None)
            .GetAwaiter()
            .GetResult();

    [Fact]
    public async Task ExecuteAsync_AsPersonalTrainer_SavesTheExercise()
    {
        var useCase = new CreateExercise(
            StubCurrentUser.PersonalTrainer(UserId.New()),
            _exercises,
            _muscleGroups,
            _ids);

        var exercise = await useCase.ExecuteAsync(
            new CreateExerciseCommand("Bench Press", _chest.Value),
            CancellationToken.None);

        Assert.Equal("Bench Press", exercise.Name);
        Assert.Equal(_chest.Value, exercise.MuscleGroupId);
        Assert.Equal("Chest", exercise.MuscleGroupName);
        Assert.True(exercise.IsActive);
        Assert.Contains(_exercises.All, saved => saved.Id.Value == exercise.Id);
    }

    [Fact]
    public async Task ExecuteAsync_AsAthlete_Fails()
    {
        var useCase = new CreateExercise(
            StubCurrentUser.Athlete(UserId.New()),
            _exercises,
            _muscleGroups,
            _ids);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(
                new CreateExerciseCommand("Bench Press", _chest.Value),
                CancellationToken.None));

        Assert.Empty(_exercises.All);
    }

    [Fact]
    public async Task ExecuteAsync_BlankName_Fails()
    {
        var useCase = new CreateExercise(
            StubCurrentUser.PersonalTrainer(UserId.New()),
            _exercises,
            _muscleGroups,
            _ids);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new CreateExerciseCommand("   ", _chest.Value), CancellationToken.None));

        Assert.Empty(_exercises.All);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutMuscleGroup_Fails()
    {
        var useCase = new CreateExercise(
            StubCurrentUser.PersonalTrainer(UserId.New()),
            _exercises,
            _muscleGroups,
            _ids);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new CreateExerciseCommand("Bench Press", Guid.Empty), CancellationToken.None));

        Assert.Equal("exercise.muscle_group.required", exception.Code);
        Assert.Empty(_exercises.All);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownMuscleGroup_Fails()
    {
        var useCase = new CreateExercise(
            StubCurrentUser.PersonalTrainer(UserId.New()),
            _exercises,
            _muscleGroups,
            _ids);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(new CreateExerciseCommand("Bench Press", Guid.NewGuid()), CancellationToken.None));

        Assert.Empty(_exercises.All);
    }
}

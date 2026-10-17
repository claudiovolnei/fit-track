using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class UpdateExerciseTests
{
    private const string OriginalName = "Bench Press";

    private readonly InMemoryExerciseRepository _exercises = new();
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly MuscleGroupId _chest = MuscleGroupId.New();
    private readonly MuscleGroupId _upperChest = MuscleGroupId.New();
    private readonly Exercise _exercise;

    public UpdateExerciseTests()
    {
        SaveMuscleGroup(_chest, "Chest");
        SaveMuscleGroup(_upperChest, "Upper Chest");

        _exercise = Exercise.Create(ExerciseId.New(), new ExerciseName(OriginalName), _chest);
        _exercises.SaveAsync(_exercise, CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ExecuteAsync_AsPersonalTrainer_UpdatesEveryEditableField()
    {
        var useCase = new UpdateExercise(
            StubCurrentUser.PersonalTrainer(UserId.New()),
            _exercises,
            _muscleGroups);

        var updated = await useCase.ExecuteAsync(
            new UpdateExerciseCommand(_exercise.Id.Value, "Incline Bench Press", _upperChest.Value, IsActive: false),
            CancellationToken.None);

        Assert.Equal("Incline Bench Press", updated.Name);
        Assert.Equal(_upperChest.Value, updated.MuscleGroupId);
        Assert.Equal("Upper Chest", updated.MuscleGroupName);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownExercise_Fails()
    {
        var useCase = new UpdateExercise(
            StubCurrentUser.PersonalTrainer(UserId.New()),
            _exercises,
            _muscleGroups);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(
                new UpdateExerciseCommand(Guid.NewGuid(), "Whatever", _chest.Value, IsActive: true),
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownMuscleGroup_Fails()
    {
        var useCase = new UpdateExercise(
            StubCurrentUser.PersonalTrainer(UserId.New()),
            _exercises,
            _muscleGroups);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(
                new UpdateExerciseCommand(_exercise.Id.Value, "Whatever", Guid.NewGuid(), IsActive: true),
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_AsAthlete_Fails()
    {
        var useCase = new UpdateExercise(
            StubCurrentUser.Athlete(UserId.New()),
            _exercises,
            _muscleGroups);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(
                new UpdateExerciseCommand(_exercise.Id.Value, "Whatever", _chest.Value, IsActive: true),
                CancellationToken.None));
    }

    private void SaveMuscleGroup(MuscleGroupId id, string name) =>
        _muscleGroups
            .SaveAsync(MuscleGroup.Create(id, new MuscleGroupName(name)), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
}

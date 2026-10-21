using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.Out;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class DeleteMuscleGroupTests
{
    private const string Name = "Chest";

    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly InMemoryCatalogUsage _usage = new();
    private readonly MuscleGroup _muscleGroup;

    public DeleteMuscleGroupTests()
    {
        _muscleGroup = MuscleGroup.Create(MuscleGroupId.New(), new MuscleGroupName(Name));
        _muscleGroups.SaveAsync(_muscleGroup, CancellationToken.None).GetAwaiter().GetResult();
    }

    private DeleteMuscleGroup UseCase(ICurrentUser currentUser) => new(currentUser, _muscleGroups, _usage);

    [Fact]
    public async Task ExecuteAsync_EmptyGroup_IsRemoved()
    {
        var useCase = UseCase(StubCurrentUser.PersonalTrainer(UserId.New()));

        await useCase.ExecuteAsync(new DeleteMuscleGroupCommand(_muscleGroup.Id.Value), CancellationToken.None);

        Assert.Null(await _muscleGroups.GetByIdAsync(_muscleGroup.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_GroupWithExercises_IsRefusedAndKept()
    {
        _usage.MuscleGroupHasExercises = true;
        var useCase = UseCase(StubCurrentUser.PersonalTrainer(UserId.New()));

        var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
            useCase.ExecuteAsync(new DeleteMuscleGroupCommand(_muscleGroup.Id.Value), CancellationToken.None));

        Assert.Equal("muscle_group.in_use", conflict.Code);
        Assert.NotNull(await _muscleGroups.GetByIdAsync(_muscleGroup.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownMuscleGroup_Fails()
    {
        var useCase = UseCase(StubCurrentUser.PersonalTrainer(UserId.New()));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(new DeleteMuscleGroupCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_AsAthlete_Fails()
    {
        var useCase = UseCase(StubCurrentUser.Athlete(UserId.New()));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new DeleteMuscleGroupCommand(_muscleGroup.Id.Value), CancellationToken.None));
    }
}

using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutCatalog;
using FitTrack.Unit.Fakes;

namespace FitTrack.Unit.Application;

public sealed class MuscleGroupTests
{
    private readonly InMemoryMuscleGroupRepository _muscleGroups = new();
    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public async Task CreateMuscleGroup_AsPersonalTrainer_SavesTheGroup()
    {
        var useCase = new CreateMuscleGroup(StubCurrentUser.PersonalTrainer(UserId.New()), _muscleGroups, _ids);

        var muscleGroup = await useCase.ExecuteAsync(new CreateMuscleGroupCommand("Chest"), CancellationToken.None);

        Assert.Equal("Chest", muscleGroup.Name);
        Assert.Contains(_muscleGroups.All, saved => saved.Id.Value == muscleGroup.Id);
    }

    [Fact]
    public async Task CreateMuscleGroup_AsAthlete_Fails()
    {
        var useCase = new CreateMuscleGroup(StubCurrentUser.Athlete(UserId.New()), _muscleGroups, _ids);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new CreateMuscleGroupCommand("Chest"), CancellationToken.None));

        Assert.Empty(_muscleGroups.All);
    }

    [Fact]
    public async Task CreateMuscleGroup_BlankName_Fails()
    {
        var useCase = new CreateMuscleGroup(StubCurrentUser.PersonalTrainer(UserId.New()), _muscleGroups, _ids);

        await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new CreateMuscleGroupCommand("  "), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMuscleGroup_AsPersonalTrainer_RenamesTheGroup()
    {
        var existing = MuscleGroup.Create(MuscleGroupId.New(), new MuscleGroupName("Chest"));
        await _muscleGroups.SaveAsync(existing, CancellationToken.None);

        var useCase = new UpdateMuscleGroup(StubCurrentUser.PersonalTrainer(UserId.New()), _muscleGroups);

        var renamed = await useCase.ExecuteAsync(
            new UpdateMuscleGroupCommand(existing.Id.Value, "Upper Chest"),
            CancellationToken.None);

        Assert.Equal("Upper Chest", renamed.Name);
    }

    [Fact]
    public async Task CreateMuscleGroup_RepeatedName_Fails()
    {
        await _muscleGroups.SaveAsync(
            MuscleGroup.Create(MuscleGroupId.New(), new MuscleGroupName("Chest")),
            CancellationToken.None);

        var useCase = new CreateMuscleGroup(StubCurrentUser.PersonalTrainer(UserId.New()), _muscleGroups, _ids);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            useCase.ExecuteAsync(new CreateMuscleGroupCommand("  Chest  "), CancellationToken.None));

        Assert.Equal("muscle_group.name.duplicate", exception.Code);
    }

    [Fact]
    public async Task UpdateMuscleGroup_UnknownGroup_Fails()
    {
        var useCase = new UpdateMuscleGroup(StubCurrentUser.PersonalTrainer(UserId.New()), _muscleGroups);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            useCase.ExecuteAsync(new UpdateMuscleGroupCommand(Guid.NewGuid(), "Chest"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMuscleGroup_KeepingItsOwnName_IsAllowed()
    {
        var existing = MuscleGroup.Create(MuscleGroupId.New(), new MuscleGroupName("Chest"));
        await _muscleGroups.SaveAsync(existing, CancellationToken.None);

        var useCase = new UpdateMuscleGroup(StubCurrentUser.PersonalTrainer(UserId.New()), _muscleGroups);

        var renamed = await useCase.ExecuteAsync(
            new UpdateMuscleGroupCommand(existing.Id.Value, "Chest"),
            CancellationToken.None);

        Assert.Equal("Chest", renamed.Name);
    }

    [Fact]
    public async Task ListMuscleGroups_ReturnsTheCatalog()
    {
        var query = new InMemoryMuscleGroupQueries();
        query.Seed(new MuscleGroupDto(Guid.NewGuid(), "Chest"));
        query.Seed(new MuscleGroupDto(Guid.NewGuid(), "Back"));

        var muscleGroups = await new ListMuscleGroups(query).ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, muscleGroups.Count);
    }
}

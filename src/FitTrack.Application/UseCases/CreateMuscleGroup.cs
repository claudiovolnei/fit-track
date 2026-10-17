using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.UseCases;

/// <summary>Registers a muscle group in the catalog. Restricted to personal trainers.</summary>
public sealed class CreateMuscleGroup : ICreateMuscleGroup
{
    private readonly ICurrentUser _currentUser;
    private readonly IMuscleGroupRepository _muscleGroups;
    private readonly IIdGenerator _idGenerator;

    public CreateMuscleGroup(
        ICurrentUser currentUser,
        IMuscleGroupRepository muscleGroups,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _muscleGroups = muscleGroups;
        _idGenerator = idGenerator;
    }

    public async Task<MuscleGroupDto> ExecuteAsync(
        CreateMuscleGroupCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsPersonalTrainer)
        {
            throw new ForbiddenException("Only personal trainers can manage the muscle groups.");
        }

        var name = new MuscleGroupName(command.Name);

        if (await _muscleGroups.NameTakenAsync(name, excluding: null, cancellationToken))
        {
            throw new DomainException(
                "muscle_group.name.duplicate",
                $"A muscle group named '{name.Value}' already exists.");
        }

        var muscleGroup = MuscleGroup.Create(
            new MuscleGroupId(_idGenerator.NewId()),
            name);

        await _muscleGroups.SaveAsync(muscleGroup, cancellationToken);

        return muscleGroup.ToDto();
    }
}

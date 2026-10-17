using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.UseCases;

/// <summary>Renames a muscle group of the catalog. Restricted to personal trainers.</summary>
public sealed class UpdateMuscleGroup : IUpdateMuscleGroup
{
    private readonly ICurrentUser _currentUser;
    private readonly IMuscleGroupRepository _muscleGroups;

    public UpdateMuscleGroup(ICurrentUser currentUser, IMuscleGroupRepository muscleGroups)
    {
        _currentUser = currentUser;
        _muscleGroups = muscleGroups;
    }

    public async Task<MuscleGroupDto> ExecuteAsync(
        UpdateMuscleGroupCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsPersonalTrainer)
        {
            throw new ForbiddenException("Only personal trainers can manage the muscle groups.");
        }

        var muscleGroup = await _muscleGroups
            .GetByIdAsync(new MuscleGroupId(command.MuscleGroupId), cancellationToken)
            ?? throw new NotFoundException($"Muscle group '{command.MuscleGroupId}' was not found.");

        var name = new MuscleGroupName(command.Name);

        if (await _muscleGroups.NameTakenAsync(name, muscleGroup.Id, cancellationToken))
        {
            throw new DomainException(
                "muscle_group.name.duplicate",
                $"A muscle group named '{name.Value}' already exists.");
        }

        muscleGroup.Rename(name);

        await _muscleGroups.SaveAsync(muscleGroup, cancellationToken);

        return muscleGroup.ToDto();
    }
}

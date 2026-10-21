using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutCatalog;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Removes a muscle group from the catalog. Restricted to personal trainers and refused while any
/// exercise still belongs to it: an exercise without its group is a dangling reference.
/// </summary>
public sealed class DeleteMuscleGroup : IDeleteMuscleGroup
{
    private readonly ICurrentUser _currentUser;
    private readonly IMuscleGroupRepository _muscleGroups;
    private readonly ICheckCatalogUsage _usage;

    public DeleteMuscleGroup(ICurrentUser currentUser, IMuscleGroupRepository muscleGroups, ICheckCatalogUsage usage)
    {
        _currentUser = currentUser;
        _muscleGroups = muscleGroups;
        _usage = usage;
    }

    public async Task ExecuteAsync(DeleteMuscleGroupCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsPersonalTrainer)
        {
            throw new ForbiddenException("Only personal trainers can manage the muscle groups.");
        }

        var muscleGroup = await _muscleGroups.GetByIdAsync(new MuscleGroupId(command.MuscleGroupId), cancellationToken)
            ?? throw new NotFoundException($"Muscle group '{command.MuscleGroupId}' was not found.");

        if (await _usage.MuscleGroupHasExercisesAsync(muscleGroup.Id, cancellationToken))
        {
            throw new ConflictException(
                "muscle_group.in_use",
                $"Muscle group '{muscleGroup.Name.Value}' still owns exercises of the catalog.");
        }

        await _muscleGroups.RemoveAsync(muscleGroup, cancellationToken);
    }
}

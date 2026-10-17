using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for renaming a muscle group of the catalog.</summary>
public interface IUpdateMuscleGroup
{
    Task<MuscleGroupDto> ExecuteAsync(UpdateMuscleGroupCommand command, CancellationToken cancellationToken);
}

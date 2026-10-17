using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for registering a muscle group in the catalog.</summary>
public interface ICreateMuscleGroup
{
    Task<MuscleGroupDto> ExecuteAsync(CreateMuscleGroupCommand command, CancellationToken cancellationToken);
}

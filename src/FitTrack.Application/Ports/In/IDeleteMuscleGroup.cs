using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for removing a muscle group of the catalog.</summary>
public interface IDeleteMuscleGroup
{
    Task ExecuteAsync(DeleteMuscleGroupCommand command, CancellationToken cancellationToken);
}

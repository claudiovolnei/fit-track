using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for removing an exercise of the catalog.</summary>
public interface IDeleteExercise
{
    Task ExecuteAsync(DeleteExerciseCommand command, CancellationToken cancellationToken);
}

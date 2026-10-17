using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for editing a catalog exercise.</summary>
public interface IUpdateExercise
{
    Task<ExerciseDto> ExecuteAsync(UpdateExerciseCommand command, CancellationToken cancellationToken);
}

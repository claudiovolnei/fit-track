using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for creating a catalog exercise.</summary>
public interface ICreateExercise
{
    Task<ExerciseDto> ExecuteAsync(CreateExerciseCommand command, CancellationToken cancellationToken);
}

using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for creating a reusable workout series.</summary>
public interface ICreateWorkoutSeries
{
    Task<WorkoutSeriesDto> ExecuteAsync(CreateWorkoutSeriesCommand command, CancellationToken cancellationToken);
}

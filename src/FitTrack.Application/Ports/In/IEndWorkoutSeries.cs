using FitTrack.Application.Commands;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for ending the series an athlete is following.</summary>
public interface IEndWorkoutSeries
{
    Task ExecuteAsync(EndWorkoutSeriesCommand command, CancellationToken cancellationToken);
}

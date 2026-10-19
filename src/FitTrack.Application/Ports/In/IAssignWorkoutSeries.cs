using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for assigning a series to an athlete.</summary>
public interface IAssignWorkoutSeries
{
    Task<AssignWorkoutSeriesResult> ExecuteAsync(AssignWorkoutSeriesCommand command, CancellationToken cancellationToken);
}

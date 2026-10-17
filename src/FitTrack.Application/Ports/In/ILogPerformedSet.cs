using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for registering a set in the workout the athlete has in progress.</summary>
public interface ILogPerformedSet
{
    Task<WorkoutSessionDto> ExecuteAsync(LogPerformedSetCommand command, CancellationToken cancellationToken);
}

using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for reading the workout history (and the time) of one athlete.</summary>
public interface IListAthleteWorkouts
{
    Task<IReadOnlyList<WorkoutSessionDto>> ExecuteAsync(
        ListAthleteWorkoutsCommand command,
        CancellationToken cancellationToken);
}

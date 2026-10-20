using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.UseCases;

/// <summary>Reads the workout history, with the measured time, of one athlete.</summary>
public sealed class ListAthleteWorkouts : IListAthleteWorkouts
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryUserSessions _sessions;

    public ListAthleteWorkouts(ICurrentUser currentUser, IQueryUserSessions sessions)
    {
        _currentUser = currentUser;
        _sessions = sessions;
    }

    public Task<IReadOnlyList<WorkoutSessionDto>> ExecuteAsync(
        ListAthleteWorkoutsCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("follow the workouts of their athletes");

        return _sessions.ListByUserAsync(new UserId(command.UserId), cancellationToken);
    }

    public Task<WorkoutHistoryDto> ExecuteAsync(
        ListAthleteWorkoutsCommand command,
        ListQuery query,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("follow the workouts of their athletes");

        return _sessions.ListByUserAsync(new UserId(command.UserId), query, cancellationToken);
    }
}

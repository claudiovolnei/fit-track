using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Reads the workout history, with the measured time, of one athlete. The administrator follows
/// everyone; the personal trainer only the athletes linked to them — history from someone else's
/// roster is denied without stating that another trainer exists at all.
/// </summary>
public sealed class ListAthleteWorkouts : IListAthleteWorkouts
{
    private readonly ICurrentUser _currentUser;
    private readonly ITrainerAthleteAssignmentRepository _links;
    private readonly IQueryUserSessions _sessions;

    public ListAthleteWorkouts(
        ICurrentUser currentUser,
        ITrainerAthleteAssignmentRepository links,
        IQueryUserSessions sessions)
    {
        _currentUser = currentUser;
        _links = links;
        _sessions = sessions;
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> ExecuteAsync(
        ListAthleteWorkoutsCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("follow the workouts of their athletes");

        var athleteId = new UserId(command.UserId);

        if (!_currentUser.IsAdmin)
        {
            await EnsureLinkedAsync(athleteId, cancellationToken);
        }

        return await _sessions.ListByUserAsync(athleteId, cancellationToken);
    }

    public async Task<WorkoutHistoryDto> ExecuteAsync(
        ListAthleteWorkoutsCommand command,
        ListQuery query,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("follow the workouts of their athletes");

        var athleteId = new UserId(command.UserId);

        if (!_currentUser.IsAdmin)
        {
            await EnsureLinkedAsync(athleteId, cancellationToken);
        }

        return await _sessions.ListByUserAsync(athleteId, query, cancellationToken);
    }

    /// <summary>
    /// The same refusal for the unlinked athlete and the one on another roster, so a curious
    /// trainer cannot tell which roster an athlete is on.
    /// </summary>
    private async Task EnsureLinkedAsync(UserId athleteId, CancellationToken cancellationToken)
    {
        var link = await _links.GetByAthleteIdAsync(athleteId, cancellationToken);

        if (link is null || link.TrainerId != _currentUser.Id)
        {
            throw new ForbiddenException(
                "A personal trainer only follows the workouts of their own athletes.");
        }
    }
}

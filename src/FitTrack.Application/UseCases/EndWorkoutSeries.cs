using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Ends the cycle the athlete is in. The link stays, marked closed, so the athlete follows nothing
/// from now on and the series they were doing stays reachable: the next cycle can start from the
/// same sheet. The workouts already done are never rewritten.
/// <para>
/// The personal trainer ends the cycle of an athlete on their own roster; an athlete released by the
/// administrator ends their own cycle.
/// </para>
/// </summary>
public sealed class EndWorkoutSeries : IEndWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly ITrainerAthleteAssignmentRepository _links;
    private readonly IWorkoutSeriesAssignmentRepository _assignments;
    private readonly IQueryUserSessions _sessions;
    private readonly IClock _clock;
    private readonly IAccountPermissions _permissions;

    public EndWorkoutSeries(
        ICurrentUser currentUser,
        ITrainerAthleteAssignmentRepository links,
        IWorkoutSeriesAssignmentRepository assignments,
        IQueryUserSessions sessions,
        IClock clock,
        IAccountPermissions permissions)
    {
        _currentUser = currentUser;
        _links = links;
        _assignments = assignments;
        _sessions = sessions;
        _clock = clock;
        _permissions = permissions;
    }

    public async Task ExecuteAsync(EndWorkoutSeriesCommand command, CancellationToken cancellationToken)
    {
        var athleteId = new UserId(command.AthleteId);

        if (_currentUser.IsPersonalTrainer)
        {
            // The roster is the gate: the trainer only acts on the athletes linked to them.
            var roster = await _links.GetByAthleteIdAsync(athleteId, cancellationToken);

            if (roster is null || roster.TrainerId != _currentUser.Id)
            {
                throw new ForbiddenException("A personal trainer only ends the series of their own athletes.");
            }
        }
        else
        {
            await WorkoutSeriesAccess.RequireAuthorAsync(
                _currentUser,
                _permissions,
                "end the series cycle of their own plan",
                cancellationToken);

            if (athleteId != _currentUser.Id)
            {
                throw new ForbiddenException("Only the athlete themselves can end their own series cycle.");
            }
        }

        var assignment = await _assignments.GetByUserIdAsync(athleteId, cancellationToken)
            ?? throw new NotFoundException($"No workout series assigned to athlete '{athleteId.Value}'.");

        if (!assignment.IsOpen)
        {
            return;
        }

        await SeriesCycleGuard.EnsureNothingRunningAsync(_sessions, athleteId, _clock.UtcNow, cancellationToken);

        assignment.Close(_clock.UtcNow);

        await _assignments.SaveAsync(assignment, cancellationToken);
    }
}

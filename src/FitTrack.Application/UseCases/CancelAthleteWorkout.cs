using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Lets a personal trainer (or an administrator) cancel a running workout of one of their
/// athletes, freeing the athlete's day. The session is removed, so it stops counting for the
/// "one workout per day" rule. A finished session is never touched, and an athlete outside the
/// caller's roster answers as if the session did not exist.
/// </summary>
public sealed class CancelAthleteWorkout : ICancelAthleteWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly ITrainerAthleteAssignmentRepository _links;
    private readonly IWorkoutSessionRepository _sessions;

    public CancelAthleteWorkout(
        ICurrentUser currentUser,
        ITrainerAthleteAssignmentRepository links,
        IWorkoutSessionRepository sessions)
    {
        _currentUser = currentUser;
        _links = links;
        _sessions = sessions;
    }

    public CancelAthleteWorkout(
        ICurrentUser currentUser,
        IWorkoutSessionRepository sessions,
        ITrainerAthleteAssignmentRepository links)
        : this(currentUser, links, sessions)
    {
    }

    public async Task ExecuteAsync(CancelAthleteWorkoutCommand command, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("cancel an athlete's workout");

        var athleteId = new UserId(command.AthleteId);

        if (!_currentUser.IsAdmin)
        {
            var link = await _links.GetByAthleteIdAsync(athleteId, cancellationToken);

            if (link is null || link.TrainerId != _currentUser.Id)
            {
                throw new NotFoundException($"Workout session '{command.SessionId}' was not found.");
            }
        }

        var session = await _sessions.GetByIdAsync(new WorkoutSessionId(command.SessionId), cancellationToken);

        if (session is null || session.UserId != athleteId)
        {
            throw new NotFoundException($"Workout session '{command.SessionId}' was not found.");
        }

        if (!session.IsRunning)
        {
            throw new ConflictException("session.cancel.not_running", "Only a running workout can be cancelled.");
        }

        if (session.HasSets)
        {
            throw new ConflictException("session.cancel.has_sets", "A workout with performed sets cannot be cancelled.");
        }

        var fresh = await _sessions.GetByIdAsync(new WorkoutSessionId(command.SessionId), cancellationToken);
        if (fresh is null || !fresh.IsRunning || fresh.HasSets)
        {
            throw new ConflictException("session.cancel.not_running", "The workout is no longer cancellable.");
        }

        await _sessions.RemoveAsync(fresh, cancellationToken);
    }
}
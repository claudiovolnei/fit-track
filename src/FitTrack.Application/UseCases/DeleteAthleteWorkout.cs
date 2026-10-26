using FitTrack.Application.Commands;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutExecution;

namespace FitTrack.Application.UseCases;

public sealed class DeleteAthleteWorkout : IDeleteAthleteWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSessionRepository _sessions;

    public DeleteAthleteWorkout(ICurrentUser currentUser, IWorkoutSessionRepository sessions)
    {
        _currentUser = currentUser;
        _sessions = sessions;
    }

    public async Task ExecuteAsync(DeleteAthleteWorkoutCommand command, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainerOrAdmin("delete a finished workout");

        var athleteId = new UserId(command.AthleteId);
        var session = await _sessions.GetByIdAsync(new WorkoutSessionId(command.SessionId), cancellationToken);

        if (session is null || session.UserId != athleteId)
        {
            throw new NotFoundException($"Workout session '{command.SessionId}' was not found.");
        }

        if (session.IsRunning)
        {
            throw new ConflictException("session.delete.not_finished", "Only a finished workout can be deleted.");
        }

        await _sessions.RemoveAsync(session, cancellationToken);
    }
}

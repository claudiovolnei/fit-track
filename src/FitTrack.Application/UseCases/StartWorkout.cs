using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutExecution;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Starts today's workout from one option of the athlete's plan. It is idempotent: if the workout for
/// that option was already started today, the existing one comes back instead of a second session.
/// An option of somebody else's plan answers as if it did not exist.
/// </summary>
public sealed class StartWorkout : IStartWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutPlanRepository _plans;
    private readonly IWorkoutSessionRepository _sessions;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public StartWorkout(
        ICurrentUser currentUser,
        IWorkoutPlanRepository plans,
        IWorkoutSessionRepository sessions,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _currentUser = currentUser;
        _plans = plans;
        _sessions = sessions;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<StartWorkoutResult> ExecuteAsync(
        StartWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        var optionId = new WorkoutPlanOptionId(command.WorkoutPlanOptionId);
        var plan = await _plans.GetByOptionIdAsync(optionId, cancellationToken);

        if (plan is null || plan.UserId != _currentUser.Id)
        {
            throw new NotFoundException($"Workout plan option '{command.WorkoutPlanOptionId}' was not found.");
        }

        var now = _clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var existing = await _sessions.GetByPlannedWorkoutAsync(_currentUser.Id, today, optionId, cancellationToken);

        if (existing is not null)
        {
            return new StartWorkoutResult(existing.ToDto(now), Started: false);
        }

        var session = WorkoutSession.StartFromPlannedWorkout(
            new WorkoutSessionId(_idGenerator.NewId()),
            _currentUser.Id,
            new WorkoutDate(today),
            now,
            optionId);

        await _sessions.SaveAsync(session, cancellationToken);

        return new StartWorkoutResult(session.ToDto(now), Started: true);
    }
}

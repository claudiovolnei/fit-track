using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Puts a series in the hands of an athlete. One series per athlete: assigning the same series again
/// changes nothing; assigning another one replaces the link and restarts the 60-day term from now.
/// Restricted to personal trainers.
/// </summary>
public sealed class AssignWorkoutSeries : IAssignWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryAthletes _athletes;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IWorkoutSeriesAssignmentRepository _assignments;
    private readonly IQueryWorkoutSeries _query;
    private readonly IIdGenerator _idGenerator;
    private readonly IClock _clock;

    public AssignWorkoutSeries(
        ICurrentUser currentUser,
        IQueryAthletes athletes,
        IWorkoutSeriesRepository series,
        IWorkoutSeriesAssignmentRepository assignments,
        IQueryWorkoutSeries query,
        IIdGenerator idGenerator,
        IClock clock)
    {
        _currentUser = currentUser;
        _athletes = athletes;
        _series = series;
        _assignments = assignments;
        _query = query;
        _idGenerator = idGenerator;
        _clock = clock;
    }

    public async Task<AssignWorkoutSeriesResult> ExecuteAsync(
        AssignWorkoutSeriesCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("assign a workout series");

        var seriesId = new WorkoutSeriesId(command.SeriesId);
        var series = await _series.GetByIdAsync(seriesId, cancellationToken);

        if (series is null || series.TrainerId != _currentUser.Id)
        {
            throw new NotFoundException($"Workout series '{command.SeriesId}' was not found.");
        }

        var athleteId = new UserId(command.UserId);

        if (!await _athletes.ExistsAsync(athleteId, cancellationToken))
        {
            throw new NotFoundException($"Athlete '{command.UserId}' was not found.");
        }

        var existing = await _assignments.GetByUserIdAsync(athleteId, cancellationToken);
        var created = existing is null;

        if (existing is null)
        {
            var assignment = WorkoutSeriesAssignment.Assign(
                new WorkoutSeriesAssignmentId(_idGenerator.NewId()),
                seriesId,
                athleteId,
                _currentUser.Id,
                _clock.UtcNow);

            await _assignments.SaveAsync(assignment, cancellationToken);
        }
        else if (existing.SeriesId != seriesId)
        {
            existing.Reassign(seriesId, _currentUser.Id, _clock.UtcNow);

            await _assignments.SaveAsync(existing, cancellationToken);
        }

        return new AssignWorkoutSeriesResult(
            await WorkoutSeriesAccess.RequireDtoAsync(_query, seriesId, cancellationToken),
            created);
    }
}

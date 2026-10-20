using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.Identity;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Creates a reusable workout series. The name is unique per trainer, checked here so the caller gets
/// a stated rule instead of the raw unique-index error. Restricted to personal trainers.
/// </summary>
public sealed class CreateWorkoutSeries : ICreateWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IQueryWorkoutSeries _query;
    private readonly IWorkoutSeriesAssignmentRepository _assignments;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public CreateWorkoutSeries(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IQueryWorkoutSeries query,
        IWorkoutSeriesAssignmentRepository assignments,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _currentUser = currentUser;
        _series = series;
        _query = query;
        _assignments = assignments;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<WorkoutSeriesDto> ExecuteAsync(
        CreateWorkoutSeriesCommand command,
        CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("create a workout series");

        var name = new WorkoutSeriesName(command.Name);

        if (await _series.NameTakenAsync(name, _currentUser.Id, excluding: null, cancellationToken))
        {
            throw new DomainException(
                "workout_series.name.duplicate",
                $"A workout series named '{name.Value}' already exists.");
        }

        var athleteId = command.AthleteId is { } athlete && athlete != Guid.Empty
            ? new UserId(athlete)
            : (UserId?)null;

        var series = WorkoutSeries.Create(
            new WorkoutSeriesId(_idGenerator.NewId()),
            name,
            _currentUser.Id,
            athleteId);

        await _series.SaveAsync(series, cancellationToken);

        // When the series is created for a specific athlete, the link and the 60-day term start
        // right away �?" there is no separate "assign" step anymore. If the athlete already had a
        // series, the existing link is reused (reassign) instead of inserting a second one.
        if (athleteId is not null)
        {
            var existing = await _assignments.GetByUserIdAsync(athleteId.Value, cancellationToken);

            WorkoutSeriesAssignment assignment;
            if (existing is null)
            {
                assignment = WorkoutSeriesAssignment.Assign(
                    new WorkoutSeriesAssignmentId(_idGenerator.NewId()),
                    series.Id,
                    athleteId.Value,
                    _currentUser.Id,
                    _clock.UtcNow);
            }
            else
            {
                existing.Reassign(series.Id, _currentUser.Id, _clock.UtcNow);
                assignment = existing;
            }

            await _assignments.SaveAsync(assignment, cancellationToken);
        }

        return await WorkoutSeriesAccess.RequireDtoAsync(_query, series.Id, cancellationToken);
    }
}

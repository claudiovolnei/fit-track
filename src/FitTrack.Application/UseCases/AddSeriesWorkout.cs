using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Mapping;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Adds a specific workout to a series. The series holds at most five (labels A..E) — the aggregate
/// states the limit. Restricted to the author of the series.
/// </summary>
public sealed class AddSeriesWorkout : IAddSeriesWorkout
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IExerciseRepository _exercises;
    private readonly IQueryWorkoutSeries _query;
    private readonly IIdGenerator _idGenerator;
    private readonly IAccountPermissions _permissions;

    public AddSeriesWorkout(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IExerciseRepository exercises,
        IQueryWorkoutSeries query,
        IIdGenerator idGenerator,
        IAccountPermissions permissions)
    {
        _currentUser = currentUser;
        _series = series;
        _exercises = exercises;
        _query = query;
        _idGenerator = idGenerator;
        _permissions = permissions;
    }

    public async Task<WorkoutSeriesWorkoutDto> ExecuteAsync(
        AddSeriesWorkoutCommand command,
        CancellationToken cancellationToken)
    {
        await WorkoutSeriesAccess.RequireAuthorAsync(
            _currentUser,
            _permissions,
            "build a workout series",
            cancellationToken);

        var series = await WorkoutSeriesAccess.RequireOwnedAsync(
            _series,
            command.SeriesId,
            _currentUser.Id,
            cancellationToken);

        var exercises = command.Exercises.ToPrescribed();

        await ExerciseLookup.RequireAllAsync(_exercises, exercises, cancellationToken);

        var workout = series.AddWorkout(
            new WorkoutSeriesWorkoutId(_idGenerator.NewId()),
            new WorkoutName(command.Name),
            exercises);

        await _series.SaveAsync(series, cancellationToken);

        var dto = await WorkoutSeriesAccess.RequireDtoAsync(_query, series.Id, cancellationToken);

        return dto.Workouts.Single(candidate => candidate.Id == workout.Id.Value);
    }
}

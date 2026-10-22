using FitTrack.Application.Commands;
using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Common;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>Renames a series. Restricted to the author of it.</summary>
public sealed class RenameWorkoutSeries : IRenameWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkoutSeriesRepository _series;
    private readonly IQueryWorkoutSeries _query;
    private readonly IAccountPermissions _permissions;

    public RenameWorkoutSeries(
        ICurrentUser currentUser,
        IWorkoutSeriesRepository series,
        IQueryWorkoutSeries query,
        IAccountPermissions permissions)
    {
        _currentUser = currentUser;
        _series = series;
        _query = query;
        _permissions = permissions;
    }

    public async Task<WorkoutSeriesDto> ExecuteAsync(
        RenameWorkoutSeriesCommand command,
        CancellationToken cancellationToken)
    {
        await WorkoutSeriesAccess.RequireAuthorAsync(
            _currentUser,
            _permissions,
            "rename a workout series",
            cancellationToken);

        var series = await WorkoutSeriesAccess.RequireOwnedAsync(
            _series,
            command.SeriesId,
            _currentUser.Id,
            cancellationToken);

        var name = new WorkoutSeriesName(command.Name);

        if (await _series.NameTakenAsync(name, _currentUser.Id, series.Id, cancellationToken))
        {
            throw new DomainException(
                "workout_series.name.duplicate",
                $"A workout series named '{name.Value}' already exists.");
        }

        series.Rename(name);

        await _series.SaveAsync(series, cancellationToken);

        return await WorkoutSeriesAccess.RequireDtoAsync(_query, series.Id, cancellationToken);
    }
}

using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Reads one series. A series authored by someone else answers as if it did not exist, so the read
/// never reveals whose athletes the other author is training.
/// </summary>
public sealed class GetWorkoutSeries : IGetWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutSeries _query;
    private readonly IAccountPermissions _permissions;

    public GetWorkoutSeries(
        ICurrentUser currentUser,
        IQueryWorkoutSeries query,
        IAccountPermissions permissions)
    {
        _currentUser = currentUser;
        _query = query;
        _permissions = permissions;
    }

    public async Task<WorkoutSeriesDto> ExecuteAsync(Guid seriesId, CancellationToken cancellationToken)
    {
        await WorkoutSeriesAccess.RequireAuthorAsync(
            _currentUser,
            _permissions,
            "see the workout series they authored",
            cancellationToken);

        var series = await _query.GetByIdAsync(new WorkoutSeriesId(seriesId), cancellationToken);

        if (series is null || series.TrainerId != _currentUser.Id.Value)
        {
            throw new NotFoundException($"Workout series '{seriesId}' was not found.");
        }

        return series;
    }
}

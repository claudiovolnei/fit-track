using FitTrack.Application.Dtos;
using FitTrack.Application.Exceptions;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.WorkoutPlanning;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Reads one series. A series authored by another trainer answers as if it did not exist, so the read
/// never reveals whose athletes the other trainer is training.
/// </summary>
public sealed class GetWorkoutSeries : IGetWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutSeries _query;

    public GetWorkoutSeries(ICurrentUser currentUser, IQueryWorkoutSeries query)
    {
        _currentUser = currentUser;
        _query = query;
    }

    public async Task<WorkoutSeriesDto> ExecuteAsync(Guid seriesId, CancellationToken cancellationToken)
    {
        _currentUser.RequirePersonalTrainer("see the workout series they authored");

        var series = await _query.GetByIdAsync(new WorkoutSeriesId(seriesId), cancellationToken);

        if (series is null || series.TrainerId != _currentUser.Id.Value)
        {
            throw new NotFoundException($"Workout series '{seriesId}' was not found.");
        }

        return series;
    }
}

using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for reading one series by id.</summary>
public interface IGetWorkoutSeries
{
    Task<WorkoutSeriesDto> ExecuteAsync(Guid seriesId, CancellationToken cancellationToken);
}

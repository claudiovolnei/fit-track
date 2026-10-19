using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for the athlete reading the series assigned to them with the state of today.</summary>
public interface IGetMyWorkoutSeries
{
    /// <summary>The series with its term, or null when the trainer has not assigned one yet.</summary>
    Task<MyWorkoutSeriesDto?> ExecuteAsync(CancellationToken cancellationToken);
}

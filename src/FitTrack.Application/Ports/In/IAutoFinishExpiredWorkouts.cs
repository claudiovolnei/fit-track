namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for closing workout sessions that have been running too long.</summary>
public interface IAutoFinishExpiredWorkouts
{
    Task<int> ExecuteAsync(CancellationToken cancellationToken);
}
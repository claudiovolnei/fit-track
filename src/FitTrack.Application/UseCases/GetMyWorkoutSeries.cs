using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Shows the athlete the series assigned to them with the state of today: for each workout, whether
/// the workout was already started, whether it is running and how long it has been going — plus the
/// days left before the term ends.
/// </summary>
public sealed class GetMyWorkoutSeries : IGetMyWorkoutSeries
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryWorkoutSeries _query;
    private readonly IClock _clock;

    public GetMyWorkoutSeries(ICurrentUser currentUser, IQueryWorkoutSeries query, IClock clock)
    {
        _currentUser = currentUser;
        _query = query;
        _clock = clock;
    }

    public Task<MyWorkoutSeriesDto?> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        return _query.GetForAthleteAsync(
            _currentUser.Id,
            DateOnly.FromDateTime(now.UtcDateTime),
            now,
            cancellationToken);
    }
}

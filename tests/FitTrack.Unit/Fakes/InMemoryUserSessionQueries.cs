using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Application.UseCases;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>Read-side fake for a user's workout history.</summary>
internal sealed class InMemoryUserSessionQueries : IQueryUserSessions
{
    private readonly List<(UserId UserId, WorkoutSessionDto Session)> _sessions = [];

    private readonly Dictionary<Guid, string> _workoutNames = [];

    /// <summary>
    /// Adds a session to the history. The workout name is what the search matches, exactly like the
    /// SQL adapter does: the session keeps pointing at the workout, the name comes from the series.
    /// </summary>
    public void Seed(UserId userId, WorkoutSessionDto session, string workoutName = "")
    {
        _sessions.Add((userId, session));
        _workoutNames[session.Id] = workoutName;
    }

    public Task<IReadOnlyList<WorkoutSessionDto>> ListByUserAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkoutSessionDto>>(
            _sessions.Where(entry => entry.UserId == userId).Select(entry => entry.Session).ToList());

    public Task<bool> HasRunningSessionAsync(
        UserId userId,
        DateOnly workoutDate,
        CancellationToken cancellationToken) =>
        Task.FromResult(_sessions.Any(entry =>
            entry.UserId == userId
            && entry.Session.WorkoutDate == workoutDate
            && entry.Session.FinishedAt is null));

    public Task<WorkoutHistoryDto> ListByUserAsync(
        UserId userId,
        ListQuery query,
        CancellationToken cancellationToken)
    {
        var sessions = _sessions
            .Where(entry => entry.UserId == userId)
            .Select(entry => entry.Session)
            .OrderByDescending(session => session.WorkoutDate)
            .ThenByDescending(session => session.StartedAt)
            .ToList();

        var page = FakePaging.Apply(sessions, query, session => _workoutNames.GetValueOrDefault(session.Id, string.Empty));

        return Task.FromResult(new WorkoutHistoryDto(
            page.Items,
            page.Page,
            page.PageSize,
            page.TotalCount,
            WorkoutVolumeCalculator.Summarize(sessions)));
    }
}

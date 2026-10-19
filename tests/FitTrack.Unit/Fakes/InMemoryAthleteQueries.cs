using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>Read-side fake for the accounts a series can be assigned to.</summary>
internal sealed class InMemoryAthleteQueries : IQueryAthletes
{
    private sealed class Entry(Guid userId, string email)
    {
        public Guid UserId { get; } = userId;

        public string Email { get; } = email;

        public Guid? SeriesId { get; set; }

        public string? SeriesName { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
    }

    private readonly List<Entry> _athletes = [];

    public void Seed(Guid userId, string email) => _athletes.Add(new Entry(userId, email));

    public void AssignSeries(Guid userId, Guid seriesId, string seriesName, DateTimeOffset expiresAt)
    {
        var entry = _athletes.Single(athlete => athlete.UserId == userId);

        entry.SeriesId = seriesId;
        entry.SeriesName = seriesName;
        entry.ExpiresAt = expiresAt;
    }

    public Task<IReadOnlyList<AthleteDto>> ListAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AthleteDto>>([.. _athletes.Select(athlete => new AthleteDto(
            athlete.UserId,
            athlete.Email,
            athlete.SeriesId,
            athlete.SeriesName,
            DaysRemaining(athlete.ExpiresAt, now)))]);

    public Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult(_athletes.Any(athlete => athlete.UserId == userId.Value));

    private static int DaysRemaining(DateTimeOffset? expiresAt, DateTimeOffset now) =>
        expiresAt is null || now >= expiresAt
            ? 0
            : (int)Math.Ceiling((expiresAt.Value - now).TotalDays);
}

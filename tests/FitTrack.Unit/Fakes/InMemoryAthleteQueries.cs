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

        public string? EndedSeriesName { get; set; }
    }

    private readonly List<Entry> _athletes = [];

    /// <summary>The trainer each athlete answers to; an athlete without a link reads as unlinked.</summary>
    private readonly Dictionary<Guid, Guid> _trainers = [];

    /// <summary>Accounts holding the personal trainer profile, for the link target check.</summary>
    private readonly HashSet<Guid> _personalTrainers = [];

    public void Seed(Guid userId, string email) => _athletes.Add(new Entry(userId, email));

    public void SeedPersonalTrainer(Guid userId) => _personalTrainers.Add(userId);

    public void AssignSeries(Guid userId, Guid seriesId, string seriesName, DateTimeOffset expiresAt)
    {
        var entry = _athletes.Single(athlete => athlete.UserId == userId);

        entry.SeriesId = seriesId;
        entry.SeriesName = seriesName;
        entry.ExpiresAt = expiresAt;
    }

    /// <summary>
    /// The cycle the trainer closed: the athlete follows nothing, and the series of the cycle stays
    /// named, the way the roster shows it.
    /// </summary>
    public void CloseSeries(Guid userId, string seriesName)
    {
        var entry = _athletes.Single(athlete => athlete.UserId == userId);

        entry.SeriesId = null;
        entry.SeriesName = null;
        entry.ExpiresAt = null;
        entry.EndedSeriesName = seriesName;
    }

    public void Link(Guid trainerId, Guid athleteId)
    {
        _trainers.Remove(athleteId);
        _trainers[athleteId] = trainerId;
    }

    public Task<IReadOnlyList<AthleteDto>> ListAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AthleteDto>>(
            [.. _athletes.Where(athlete => !_trainers.ContainsKey(athlete.UserId))
                .Select(athlete => ToDto(athlete, now))]);

    public Task<PagedResult<AthleteDto>> ListAsync(
        DateTimeOffset now,
        ListQuery query,
        CancellationToken cancellationToken) =>
        Task.FromResult(FakePaging.Apply(
            _athletes
                .Select(athlete => ToDto(athlete, now, _trainers.GetValueOrDefault(athlete.UserId)))
                .OrderBy(athlete => athlete.Email),
            query,
            athlete => athlete.Email));

    public Task<IReadOnlyList<AthleteDto>> ListAsync(
        DateTimeOffset now,
        UserId trainerId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AthleteDto>>(
        [
            .. _athletes
                .Where(athlete => _trainers.GetValueOrDefault(athlete.UserId) == trainerId.Value)
                .Select(athlete => ToDto(athlete, now)),
        ]);

    public Task<PagedResult<AthleteDto>> ListAsync(
        DateTimeOffset now,
        ListQuery query,
        UserId trainerId,
        CancellationToken cancellationToken) =>
        Task.FromResult(FakePaging.Apply(
            _athletes
                .Where(athlete => _trainers.GetValueOrDefault(athlete.UserId) == trainerId.Value)
                .Select(athlete => ToDto(athlete, now))
                .OrderBy(athlete => athlete.Email),
            query,
            athlete => athlete.Email));

    public Task<PagedResult<AthleteDto>> ListUnlinkedAsync(
        DateTimeOffset now,
        ListQuery query,
        CancellationToken cancellationToken) =>
        Task.FromResult(FakePaging.Apply(
            _athletes
                .Where(athlete => !_trainers.ContainsKey(athlete.UserId))
                .Select(athlete => ToDto(athlete, now))
                .OrderBy(athlete => athlete.Email),
            query,
            athlete => athlete.Email));

    public Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult(_athletes.Any(athlete => athlete.UserId == userId.Value));

    public Task<bool> ExistsPersonalTrainerAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult(_personalTrainers.Contains(userId.Value));

    private static AthleteDto ToDto(Entry athlete, DateTimeOffset now) =>
        new(
            athlete.UserId,
            athlete.Email,
            athlete.SeriesId,
            athlete.SeriesName,
            DaysRemaining(athlete.ExpiresAt, now),
            PersonalTrainerEmail: null,
            EndedSeriesName: athlete.EndedSeriesName);

    private static AthleteDto ToDto(Entry athlete, DateTimeOffset now, Guid trainerId) =>
        trainerId == Guid.Empty
            ? ToDto(athlete, now)
            : ToDto(athlete, now) with { PersonalTrainerEmail = $"trainer-{trainerId}@fake.local" };

    private static int DaysRemaining(DateTimeOffset? expiresAt, DateTimeOffset now) =>
        expiresAt is null || now >= expiresAt
            ? 0
            : (int)Math.Ceiling((expiresAt.Value - now).TotalDays);
}

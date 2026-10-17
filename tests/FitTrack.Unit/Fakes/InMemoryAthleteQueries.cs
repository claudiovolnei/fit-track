using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;
using FitTrack.Domain.Identity;

namespace FitTrack.Unit.Fakes;

/// <summary>Read-side fake for the accounts a plan can be assigned to.</summary>
internal sealed class InMemoryAthleteQueries : IQueryAthletes
{
    private readonly List<AthleteDto> _athletes = [];

    public void Seed(Guid userId, string email, bool hasPlan = false) =>
        _athletes.Add(new AthleteDto(userId, email, hasPlan));

    public Task<IReadOnlyList<AthleteDto>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AthleteDto>>(_athletes);

    public Task<bool> ExistsAsync(UserId userId, CancellationToken cancellationToken) =>
        Task.FromResult(_athletes.Any(athlete => athlete.UserId == userId.Value));
}

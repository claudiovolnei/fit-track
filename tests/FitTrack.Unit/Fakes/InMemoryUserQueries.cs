using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Unit.Fakes;

/// <summary>Read-side fake for the accounts the administrator manages.</summary>
internal sealed class InMemoryUserQueries : IQueryUsers
{
    private readonly List<UserAccountDto> _accounts = [];

    public bool WasQueried { get; private set; }

    public void Seed(Guid userId, string name, string email, params string[] roles) =>
        _accounts.Add(new UserAccountDto(userId, name, email, roles));

    /// <summary>Seeds an account the administrator released to build its own training plan.</summary>
    public void SeedReleased(Guid userId, string name, string email, params string[] roles) =>
        _accounts.Add(new UserAccountDto(userId, name, email, roles) { CanAuthorOwnWorkout = true });

    public Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken)
    {
        WasQueried = true;

        return Task.FromResult<IReadOnlyList<UserAccountDto>>(
            [.. _accounts.OrderBy(account => account.Email)]);
    }

    public Task<PagedResult<UserAccountDto>> ListAsync(
        ListQuery query,
        string? role,
        CancellationToken cancellationToken)
    {
        WasQueried = true;

        var filtered = string.IsNullOrWhiteSpace(role)
            ? _accounts
            : _accounts.Where(account => account.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));

        return Task.FromResult(FakePaging.Apply(
            filtered.OrderBy(account => account.Email),
            query,
            account => $"{account.Name} {account.Email}"));
    }
}

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

    public Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken)
    {
        WasQueried = true;

        return Task.FromResult<IReadOnlyList<UserAccountDto>>(
            [.. _accounts.OrderBy(account => account.Email)]);
    }
}

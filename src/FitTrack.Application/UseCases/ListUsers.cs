using FitTrack.Application.Dtos;
using FitTrack.Application.Ports.In;
using FitTrack.Application.Ports.Out;

namespace FitTrack.Application.UseCases;

/// <summary>
/// Lists every account with the profiles it holds. Restricted to administrators: the create side is
/// one store call the inbound adapter already drives, but the list is a read of its own.
/// </summary>
public sealed class ListUsers : IListUsers
{
    private readonly ICurrentUser _currentUser;
    private readonly IQueryUsers _users;

    public ListUsers(ICurrentUser currentUser, IQueryUsers users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public Task<IReadOnlyList<UserAccountDto>> ExecuteAsync(CancellationToken cancellationToken)
    {
        _currentUser.RequireAdmin("list the accounts");

        return _users.ListAsync(cancellationToken);
    }
}

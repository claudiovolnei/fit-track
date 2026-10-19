using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the accounts, feeding the administration screen.</summary>
public interface IListUsers
{
    Task<IReadOnlyList<UserAccountDto>> ExecuteAsync(CancellationToken cancellationToken);
}

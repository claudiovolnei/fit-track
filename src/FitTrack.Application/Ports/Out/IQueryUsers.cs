using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the accounts the administrator manages.</summary>
public interface IQueryUsers
{
    /// <summary>Every account with the profiles it holds, ordered by e-mail.</summary>
    Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken);

    Task<PagedResult<UserAccountDto>> ListAsync(ListQuery query, CancellationToken cancellationToken);
}

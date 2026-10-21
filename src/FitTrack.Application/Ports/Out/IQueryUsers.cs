using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.Out;

/// <summary>Read-side port for the accounts the administrator manages.</summary>
public interface IQueryUsers
{
    /// <summary>Every account with the profiles it holds, ordered by e-mail.</summary>
    Task<IReadOnlyList<UserAccountDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>The same page, restricted to the accounts holding <paramref name="role"/>, when named.</summary>
    Task<PagedResult<UserAccountDto>> ListAsync(ListQuery query, string? role, CancellationToken cancellationToken);
}

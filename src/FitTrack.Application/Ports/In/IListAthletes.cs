using FitTrack.Application.Dtos;

namespace FitTrack.Application.Ports.In;

/// <summary>Driving port for listing the athletes a plan can be assigned to.</summary>
public interface IListAthletes
{
    Task<IReadOnlyList<AthleteDto>> ExecuteAsync(CancellationToken cancellationToken);

    Task<PagedResult<AthleteDto>> ExecuteAsync(ListQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// "<c>unlinkedOnly</c>" reads the candidates: athletes still linked to nobody, which is what
    /// the link screen of either profile searches through.
    /// </summary>
    Task<PagedResult<AthleteDto>> ExecuteAsync(
        ListQuery query,
        bool unlinkedOnly,
        CancellationToken cancellationToken);
}

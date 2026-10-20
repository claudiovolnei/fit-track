using FitTrack.Application.Dtos;

namespace FitTrack.Unit.Fakes;

/// <summary>Applies the same search/page contract as the SQL adapters on top of an in-memory list.</summary>
internal static class FakePaging
{
    public static PagedResult<T> Apply<T>(
        IEnumerable<T> source,
        ListQuery query,
        Func<T, string> searchText)
    {
        IEnumerable<T> filtered = source;

        if (query.Search is { } term)
        {
            filtered = filtered.Where(item =>
                searchText(item).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var all = filtered.ToList();
        var items = query.IsPaged
            ? all.Skip(query.Skip).Take(query.Take).ToList()
            : all;

        return new PagedResult<T>(items, query.Page, query.PageSize, all.Count);
    }
}

namespace FitTrack.Adapters.Out.Persistence.Queries;

/// <summary>
/// Builds the "contains" argument for <c>EF.Functions.ILike</c>. The term is trimmed and the three
/// characters the pattern language gives meaning to are escaped, so a search for "50%" looks for the
/// text and not for anything ending in 50.
/// </summary>
internal static class SearchPattern
{
    public static string Contains(string term) =>
        $"%{term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}

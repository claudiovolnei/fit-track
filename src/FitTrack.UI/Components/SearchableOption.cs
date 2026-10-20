namespace FitTrack.UI.Components;

/// <summary>One option of a <see cref="SearchableSelect"/>: the id sent to the API and the text shown.</summary>
public sealed record SearchableOption(Guid Id, string Label, string? Hint = null);

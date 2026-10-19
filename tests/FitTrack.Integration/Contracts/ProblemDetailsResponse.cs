namespace FitTrack.Integration.Contracts;

/// <summary>
/// The refusal body the API returns. The <c>title</c> is the stable code of the rule that was broken and
/// the <c>detail</c> is the human sentence; <c>errors</c> only shows up on a validation failure.
/// </summary>
public sealed record ProblemDetailsResponse(
    string Title,
    string? Detail,
    int? Status,
    IReadOnlyDictionary<string, string[]>? Errors);

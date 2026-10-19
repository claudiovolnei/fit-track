using Microsoft.AspNetCore.Identity;

namespace FitTrack.Adapters.In.Api.Composition;

/// <summary>
/// What the two inbound adapters that write accounts — self-registration and the administrator's
/// screen — answer when the input does not hold. Identity stays in the inbound adapter for writes
/// (the store already drives it), so the shape of its refusal is shared here instead of copied.
/// </summary>
internal static class AccountProblems
{
    public const int MaxNameLength = 80;

    /// <summary>
    /// The problem with the display name, or <c>null</c> when it is acceptable. The name is the column
    /// the screens read, so it is required and bounded here as well as on the form.
    /// </summary>
    public static IResult? ProblemWithName(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? Invalid("Name", "Name is required.")
            : name.Trim().Length > MaxNameLength
                ? Invalid("Name", $"Name must be at most {MaxNameLength} characters.")
                : null;

    public static IResult Invalid(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>The store's refusal, one entry per Identity error code so the screen can name the field.</summary>
    public static IResult Validation(IdentityResult result) =>
        Results.ValidationProblem(result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray()));

    /// <summary>For the calls that change nothing but a role: success is <c>204</c>, refusal is a problem.</summary>
    public static IResult Store(IdentityResult result) =>
        result.Succeeded ? Results.NoContent() : Validation(result);
}

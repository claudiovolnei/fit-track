using System.Net;

namespace FitTrack.UI.Api;

/// <summary>
/// Error returned by the API. Carries the rule code (the <c>title</c> of the <c>ProblemDetails</c>), the
/// server message and, on validation problems, the message for each field.
/// </summary>
public sealed class ApiException : Exception
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new Dictionary<string, string[]>();

    public ApiException(
        HttpStatusCode statusCode,
        string message,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Errors = errors ?? NoErrors;
    }

    public HttpStatusCode StatusCode { get; }

    /// <summary>The rule code that refused the action, when the API sends one.</summary>
    public string? Code { get; }

    /// <summary>Messages per field, on validation problems.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

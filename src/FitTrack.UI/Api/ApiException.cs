using System.Net;

namespace FitTrack.UI.Api;

/// <summary>Raised when the API answers with a non-success status. Carries the server message.</summary>
public sealed class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

using System.Net;

namespace FitTrack.UI.Api;

/// <summary>
/// Erro devolvido pela API. Carrega o código da regra (o <c>title</c> do <c>ProblemDetails</c>), a
/// mensagem do servidor e, nos problemas de validação, a mensagem de cada campo.
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

    /// <summary>Código da regra que recusou a ação, quando a API manda um.</summary>
    public string? Code { get; }

    /// <summary>Mensagens por campo, nos problemas de validação.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

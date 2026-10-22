using System.Net;
using System.Text;
using System.Text.Json;

namespace FitTrack.UI.Tests;

/// <summary>One request the screen made: the method, the path and the body it carried.</summary>
public sealed record RecordedRequest(string Method, string Path, string? Body);

/// <summary>
/// The API of the app answered by hand. It records every request and hands back the canned answers
/// the screen expects — the point of the suite is to see what the screen asks for, and, above all,
/// what it does <em>not</em> ask for.
/// </summary>
public sealed class FakeApi : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<(string Method, string Path), Func<HttpRequestMessage, HttpResponseMessage>> _answers = [];

    /// <summary>Every request the screen made, in order.</summary>
    public List<RecordedRequest> Requests { get; } = [];

    /// <summary>The requests to a path, in order.</summary>
    public IReadOnlyList<RecordedRequest> RequestsTo(string path) =>
        [.. Requests.Where(request => request.Path == path)];

    /// <summary>Answers a route with a payload.</summary>
    public FakeApi Answer(HttpMethod method, string path, object payload) =>
        Answer(method, path, _ => Json(HttpStatusCode.OK, JsonSerializer.Serialize(payload, Web)));
    /// <summary>Answers a route with nothing (the API answers 204 when there is no series).</summary>
    public FakeApi AnswerEmpty(HttpMethod method, string path) =>
        Answer(method, path, _ => new HttpResponseMessage(HttpStatusCode.NoContent));

    /// <summary>Answers a route with a refusal: the status and the code the API carries.</summary>
    public FakeApi AnswerProblem(HttpMethod method, string path, HttpStatusCode status, string code) =>
        Answer(method, path, _ => Json(status, JsonSerializer.Serialize(new { title = code }, Web)));

    /// <summary>Answers a route with a body the test builds per request.</summary>
    public FakeApi Answer(
        HttpMethod method,
        string path,
        Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        _answers[(method.Method, path)] = answer;

        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var path = request.RequestUri!.AbsolutePath;

        Requests.Add(new RecordedRequest(request.Method.Method, path, body));

        if (_answers.TryGetValue((request.Method.Method, path), out var answer))
        {
            return answer(request);
        }

        return Json(
            HttpStatusCode.NotFound,
            JsonSerializer.Serialize(new { title = "not_found", detail = "No route." }, Web));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}

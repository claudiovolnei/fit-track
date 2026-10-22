using FitTrack.UI.Notifications;

namespace FitTrack.UI.Api;

/// <summary>
/// Reports every request to the activity counter, so the screen is blocked while the server answers.
/// It sits in the pipeline of the typed client, which is the only door the screens have to the API.
/// </summary>
internal sealed class RequestActivityHandler(RequestActivity activity) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var scope = activity.Track();

        return await base.SendAsync(request, cancellationToken);
    }
}

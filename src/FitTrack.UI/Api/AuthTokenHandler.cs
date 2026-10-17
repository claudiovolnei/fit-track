using System.Net.Http.Headers;
using FitTrack.UI.Auth;

namespace FitTrack.UI.Api;

/// <summary>Attaches the stored bearer token to every outgoing API call.</summary>
public sealed class AuthTokenHandler(ITokenStore tokenStore) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

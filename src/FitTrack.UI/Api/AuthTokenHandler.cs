using System.Net;
using System.Net.Http.Headers;
using FitTrack.UI.Auth;
using Microsoft.AspNetCore.Components;

namespace FitTrack.UI.Api;

/// <summary>
/// Attaches the stored bearer token to every outgoing API call and centralises the reaction to an
/// expired/invalid session: when the API answers 401, the local token is cleared and the user is
/// sent back to the login page. A shared gate guarantees a single redirect even when several
/// requests fail at the same time.
/// </summary>
public sealed class AuthTokenHandler(
    ITokenStore tokenStore,
    UserSession session,
    NavigationManager navigation) : DelegatingHandler
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenStore.GetAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await Gate.WaitAsync(cancellationToken);
            try
            {
                // Only act when there is still a token to remove: this keeps concurrent 401s
                // from triggering more than one sign-out/navigation.
                var current = await tokenStore.GetAsync();
                if (!string.IsNullOrWhiteSpace(current))
                {
                    try
                    {
                        await session.SignOutAsync();
                    }
                    catch
                    {
                        // Best effort: never let a storage failure block the redirect.
                        await tokenStore.ClearAsync();
                    }

                    if (!IsPublicPage())
                    {
                        var returnUrl = Uri.EscapeDataString(navigation.ToBaseRelativePath(navigation.Uri));
                        navigation.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: false);
                    }
                }
            }
            finally
            {
                Gate.Release();
            }
        }

        return response;
    }

    private bool IsPublicPage()
    {
        var path = new Uri(navigation.Uri).AbsolutePath;
        return path.Equals("/login", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/register", StringComparison.OrdinalIgnoreCase);
    }
}
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitTrack.UI.Auth;

/// <summary>Builds the client-side authentication state from the stored access token.</summary>
public sealed class TokenAuthenticationStateProvider(ITokenStore tokenStore) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private int _clearing;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        if (JwtReader.IsExpired(token, DateTimeOffset.UtcNow))
        {
            await ClearExpiredTokenAsync();
            return Anonymous;
        }

        var claims = JwtReader.ReadClaims(token);
        if (claims.Count == 0)
        {
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt", ClaimTypes.Name, ClaimTypes.Role);

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    private async Task ClearExpiredTokenAsync()
    {
        if (Interlocked.Exchange(ref _clearing, 1) == 1)
        {
            return;
        }

        try
        {
            await tokenStore.ClearAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _clearing, 0);
        }

        // Re-evaluate without re-entering the clearing path.
        NotifyChanged();
    }
}

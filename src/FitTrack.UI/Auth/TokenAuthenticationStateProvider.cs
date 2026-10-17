using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace FitTrack.UI.Auth;

/// <summary>Builds the client-side authentication state from the stored access token.</summary>
public sealed class TokenAuthenticationStateProvider(ITokenStore tokenStore) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
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
}

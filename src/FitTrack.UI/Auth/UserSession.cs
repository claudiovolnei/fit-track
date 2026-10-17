namespace FitTrack.UI.Auth;

/// <summary>Signs the user in and out by moving the access token and refreshing the auth state.</summary>
public sealed class UserSession(TokenAuthenticationStateProvider authenticationState, ITokenStore tokenStore)
{
    public async Task SignInAsync(string accessToken)
    {
        await tokenStore.SaveAsync(accessToken);
        authenticationState.NotifyChanged();
    }

    public async Task SignOutAsync()
    {
        await tokenStore.ClearAsync();
        authenticationState.NotifyChanged();
    }
}

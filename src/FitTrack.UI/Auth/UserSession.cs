namespace FitTrack.UI.Auth;

/// <summary>Signs the user in and out by moving the access token and refreshing the auth state.</summary>
public sealed class UserSession(
    TokenAuthenticationStateProvider authenticationState,
    ITokenStore tokenStore,
    UserAvatarState avatarState)
{
    public async Task SignInAsync(string accessToken)
    {
        // The SPA survives the login, so the menu is not recreated: forget any avatar from a
        // previous account and let the authenticated layout fetch this user's image.
        avatarState.Clear();
        await tokenStore.SaveAsync(accessToken);
        authenticationState.NotifyChanged();
    }

    public async Task SignOutAsync()
    {
        avatarState.Clear();
        await tokenStore.ClearAsync();
        authenticationState.NotifyChanged();
    }
}

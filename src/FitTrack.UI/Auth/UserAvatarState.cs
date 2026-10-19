namespace FitTrack.UI.Auth;

/// <summary>
/// Holds the avatar data URL of the signed-in user for the whole SPA, so the menu icon and the
/// account screen stay in sync after an upload or a removal without reloading the page.
/// </summary>
public sealed class UserAvatarState
{
    public string? AvatarUrl { get; private set; }

    public event Action? Changed;

    public void Set(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        Changed?.Invoke();
    }
}

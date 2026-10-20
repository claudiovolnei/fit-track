namespace FitTrack.UI.Auth;

/// <summary>
/// Holds the avatar data URL of the signed-in user for the whole SPA, so the menu icon and the
/// account screen stay in sync after an upload or a removal without reloading the page. The state
/// is tied to the user key, so a different account never inherits the previous avatar, and a
/// superseded load can never overwrite a newer one.
/// </summary>
public sealed class UserAvatarState
{
    private readonly object _gate = new();
    private Task<string?>? _loading;
    private string? _loadingKey;
    private CancellationTokenSource? _cancellation;
    private int _generation;

    public string? UserKey { get; private set; }

    public string? AvatarUrl { get; private set; }

    public bool Loaded { get; private set; }

    public event Action? Changed;

    /// <summary>
    /// Loads the avatar once per signed-in user. Concurrent callers share the same fetch, and a
    /// caller that arrives while a previous user is still loading supersedes that fetch.
    /// </summary>
    public async Task EnsureLoadedAsync(
        string? userKey,
        Func<CancellationToken, Task<string?>> load,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userKey) || load is null)
        {
            return;
        }

        Task<string?> inFlight;

        lock (_gate)
        {
            if (Loaded && string.Equals(UserKey, userKey, StringComparison.Ordinal))
            {
                return;
            }

            if (_loading is not null && string.Equals(_loadingKey, userKey, StringComparison.Ordinal))
            {
                inFlight = _loading;
            }
            else
            {
                InvalidateLocked();

                var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _cancellation = cancellation;
                _loadingKey = userKey;
                inFlight = LoadAsync(userKey, _generation, load, cancellation.Token);
                _loading = inFlight;
            }
        }

        try
        {
            await inFlight.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A newer user or a sign-out superseded this fetch.
        }
    }

    /// <summary>
    /// Applies an avatar chosen by the current user (upload or removal). The user key travels with
    /// the avatar so the next <see cref="EnsureLoadedAsync"/> recognizes the state as the current
    /// user's and does not fetch it again.
    /// </summary>
    public void Set(string? userKey, string? avatarUrl)
    {
        lock (_gate)
        {
            InvalidateLocked();
            UserKey = userKey;
            AvatarUrl = avatarUrl;
            Loaded = true;
        }

        Changed?.Invoke();
    }

    /// <summary>Forgets the current user, so the next sign-in always fetches its own avatar.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            InvalidateLocked();
            UserKey = null;
            AvatarUrl = null;
            Loaded = false;
        }

        Changed?.Invoke();
    }

    private async Task<string?> LoadAsync(
        string userKey,
        int generation,
        Func<CancellationToken, Task<string?>> load,
        CancellationToken cancellationToken)
    {
        string? avatarUrl = null;
        var cancelled = false;

        try
        {
            avatarUrl = await load(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch
        {
            // A failed fetch falls back to the initials until the next authentication change.
            avatarUrl = null;
        }

        var changed = false;

        lock (_gate)
        {
            if (generation != _generation)
            {
                return null;
            }

            _loading = null;
            _loadingKey = null;

            if (!cancelled)
            {
                UserKey = userKey;
                AvatarUrl = avatarUrl;
                Loaded = true;
            }

            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }

        return avatarUrl;
    }

    private void InvalidateLocked()
    {
        _generation++;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        _loading = null;
        _loadingKey = null;
    }
}

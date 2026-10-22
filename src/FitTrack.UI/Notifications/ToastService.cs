namespace FitTrack.UI.Notifications;

/// <summary>How loud a message is: the colour and the icon the toast carries.</summary>
public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>One message waiting in the corner of the screen.</summary>
public sealed record ToastMessage(Guid Id, ToastLevel Level, string Text, DateTimeOffset ExpiresAt);

/// <summary>
/// The toast hub of the client. The screens report what happened and the host draws it in one place,
/// so an error is always seen in the same corner instead of inside the screen that produced it.
/// </summary>
public sealed class ToastService
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan ErrorLifetime = TimeSpan.FromSeconds(9);

    private readonly List<ToastMessage> _messages = [];

    private readonly TimeProvider _time;

    public ToastService()
        : this(TimeProvider.System)
    {
    }

    public ToastService(TimeProvider timeProvider) => _time = timeProvider;

    /// <summary>Raised whenever a message is added or removed, so the host draws the change.</summary>
    public event Action? Changed;

    /// <summary>The messages on the screen, oldest first.</summary>
    public IReadOnlyList<ToastMessage> Messages => _messages;

    /// <summary>Shows a message. The text of an empty message is ignored.</summary>
    public ToastMessage Show(string text, ToastLevel level = ToastLevel.Info, TimeSpan? lifetime = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A toast needs a text.", nameof(text));
        }

        var message = new ToastMessage(
            Guid.NewGuid(),
            level,
            text.Trim(),
            _time.GetUtcNow() + (lifetime ?? (level == ToastLevel.Error ? ErrorLifetime : DefaultLifetime)));

        _messages.Add(message);
        Changed?.Invoke();

        return message;
    }

    public ToastMessage Info(string text) => Show(text, ToastLevel.Info);

    public ToastMessage Success(string text) => Show(text, ToastLevel.Success);

    public ToastMessage Warning(string text) => Show(text, ToastLevel.Warning);

    public ToastMessage Error(string text) => Show(text, ToastLevel.Error);

    /// <summary>Takes one message off the screen.</summary>
    public void Dismiss(Guid id)
    {
        var removed = _messages.RemoveAll(message => message.Id == id);

        if (removed > 0)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>Takes every message off the screen.</summary>
    public void Clear()
    {
        if (_messages.Count == 0)
        {
            return;
        }

        _messages.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// Takes the messages whose time is up. The host calls it on its own tick, so a message leaves the
    /// screen on its own without any screen having to remember it.
    /// </summary>
    public int RemoveExpired()
    {
        var now = _time.GetUtcNow();
        var removed = _messages.RemoveAll(message => message.ExpiresAt <= now);

        if (removed > 0)
        {
            Changed?.Invoke();
        }

        return removed;
    }
}

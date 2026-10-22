namespace FitTrack.UI.Notifications;

/// <summary>
/// Counts the requests in flight so the screen can be blocked while the server answers. It is a
/// counter and not a flag: two requests at once keep the screen blocked until both are done.
/// </summary>
public sealed class RequestActivity
{
    private int _pending;

    /// <summary>Whether a request is in flight right now.</summary>
    public bool IsBusy => Volatile.Read(ref _pending) > 0;

    /// <summary>How many requests are in flight right now.</summary>
    public int Pending => Volatile.Read(ref _pending);

    /// <summary>Raised when a request starts or ends, so the overlay can appear and disappear.</summary>
    public event Action? Changed;

    /// <summary>
    /// Starts counting one request. Disposing what it returns ends the count, even when the request
    /// failed.
    /// </summary>
    public IDisposable Track()
    {
        Interlocked.Increment(ref _pending);
        Changed?.Invoke();

        return new Scope(this);
    }

    private void Release()
    {
        if (Interlocked.Decrement(ref _pending) < 0)
        {
            // The count never goes below zero: a scope released twice must not unblock the screen
            // while another request is still on its way.
            Interlocked.Exchange(ref _pending, 0);
        }

        Changed?.Invoke();
    }

    private sealed class Scope(RequestActivity activity) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                activity.Release();
            }
        }
    }
}

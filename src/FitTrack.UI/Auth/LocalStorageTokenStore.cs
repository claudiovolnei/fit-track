using Microsoft.JSInterop;

namespace FitTrack.UI.Auth;

/// <summary>Browser implementation backed by <c>localStorage</c>.</summary>
public sealed class LocalStorageTokenStore(IJSRuntime jsRuntime) : ITokenStore
{
    private const string StorageKey = "fittrack.accessToken";

    public async ValueTask<string?> GetAsync() =>
        await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);

    public async ValueTask SaveAsync(string token) =>
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, token);

    public async ValueTask ClearAsync() =>
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
}

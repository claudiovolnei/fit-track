using Bunit;
using FitTrack.UI.Api;
using FitTrack.UI.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.UI.Tests;

/// <summary>
/// The base of the screen tests: the app's own services over a fake API, so a screen can be drawn
/// and driven without a server.
/// </summary>
public abstract class ScreenTest : Bunit.BunitContext
{
    protected FakeApi Api { get; } = new();

    protected ToastService Toasts { get; } = new();

    protected ScreenTest()
    {
        Services.AddSingleton(Api);
        Services.AddHttpClient<FitTrackApiClient>(client => client.BaseAddress = new Uri("http://api.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => Api);
        Services.AddSingleton(Toasts);
        Services.AddSingleton<RequestActivity>();
    }
}

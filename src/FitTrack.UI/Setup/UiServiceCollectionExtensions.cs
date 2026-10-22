using FitTrack.UI.Api;
using FitTrack.UI.Auth;
using FitTrack.UI.Notifications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.UI.Setup;

/// <summary>
/// Wires the shared UI into a host. The host supplies the API base address and the
/// platform-specific <see cref="ITokenStore"/>.
/// </summary>
public static class UiServiceCollectionExtensions
{
    public static IServiceCollection AddFitTrackUi(this IServiceCollection services, Uri apiBaseUrl)
    {
        services.AddAuthorizationCore();
        services.AddCascadingAuthenticationState();

        services.AddScoped<TokenAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<TokenAuthenticationStateProvider>());
        services.AddScoped<UserSession>();
        services.AddScoped<UserAvatarState>();

        // The messages of the screens and the requests in flight: both live for the whole session.
        // The activity is a singleton on purpose: the handler of the HTTP pipeline resolves it from
        // its own scope, and only the root instance is the one the overlay watches.
        services.AddScoped<ToastService>();
        services.AddSingleton<RequestActivity>();

        services.AddTransient<AuthTokenHandler>();
        services.AddTransient<RequestActivityHandler>();
        services.AddHttpClient<FitTrackApiClient>(client => client.BaseAddress = apiBaseUrl)
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddHttpMessageHandler<RequestActivityHandler>();

        return services;
    }
}

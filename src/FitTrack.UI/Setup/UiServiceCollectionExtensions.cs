using FitTrack.UI.Api;
using FitTrack.UI.Auth;
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

        services.AddTransient<AuthTokenHandler>();
        services.AddHttpClient<FitTrackApiClient>(client => client.BaseAddress = apiBaseUrl)
            .AddHttpMessageHandler<AuthTokenHandler>();

        return services;
    }
}

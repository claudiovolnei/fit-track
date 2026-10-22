using FitTrack.Application.Ports.In;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitTrack.Adapters.In.Api.HostedServices;

/// <summary>
/// Closes forgotten workouts every five minutes, so the athlete is never blocked by a session
/// that was left running.
/// </summary>
internal sealed class AutoFinishExpiredWorkoutsHostedService(
    IServiceProvider serviceProvider,
    ILogger<AutoFinishExpiredWorkoutsHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<IAutoFinishExpiredWorkouts>();
                await useCase.ExecuteAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Auto-finish expired workouts failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Identity.Infrastructure.Persistence;

namespace Nexo.Identity.Infrastructure.Messaging;

/// <summary>Revisa las bajas pendientes. El lease por cuenta permite que haya varias réplicas.</summary>
public sealed class AccountLifecycleWorker(IServiceScopeFactory scopes, ILogger<AccountLifecycleWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AccountLifecycleCoordinator>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                logger.LogWarning("No se pudieron revisar las bajas pendientes ({Error}); se reintentará.", error.GetType().Name);
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace Nexo.Discovery.Infrastructure.Media;
public sealed class MediaWorker(IServiceScopeFactory scopes,AzureMediaStorage storage,IOptions<MediaOptions> options,ILogger<MediaWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(string.IsNullOrWhiteSpace(options.Value.ConnectionString)) return;
        await storage.Queue.CreateIfNotExistsAsync(cancellationToken:stoppingToken);
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope=scopes.CreateScope();var service=scope.ServiceProvider.GetRequiredService<MongoMediaService>();
                if(!options.Value.DisableMaintenance)
                {
                    await service.RecoverAsync(stoppingToken);
                    await service.AuditAsync(stoppingToken);
                }
                var messages=(await storage.Queue.ReceiveMessagesAsync(4,TimeSpan.FromMinutes(3),stoppingToken)).Value;
                foreach(var message in messages)
                {
                    if(Guid.TryParse(message.Body.ToString(),out var id)) await service.ProcessAsync(id,stoppingToken);
                    await storage.Queue.DeleteMessageAsync(message.MessageId,message.PopReceipt,stoppingToken);
                }
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
            catch(Exception) { log.LogWarning("El procesamiento de imagenes reintentara por dependencia no disponible."); }
            try { await Task.Delay(TimeSpan.FromSeconds(3),stoppingToken); } catch(OperationCanceledException) { break; }
        }
    }
}

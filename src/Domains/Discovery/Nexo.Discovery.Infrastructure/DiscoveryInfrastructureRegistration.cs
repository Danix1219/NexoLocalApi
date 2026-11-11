using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Infrastructure.Providers;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Infrastructure.Directory;

namespace Nexo.Discovery.Infrastructure;

public static class DiscoveryInfrastructureRegistration
{
    /// <summary>
    /// Adaptadores de Discovery; recibe la base keyed y nunca un cliente global.
    /// Perfiles moderados y ubicacion. No resuelve bases de otros dominios.
    /// </summary>
    public static IServiceCollection AddDiscoveryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<Nexo.Discovery.Application.ISearchLocationStore, MongoSearchLocationStore>();
        services.AddOptions<GeocodingOptions>().Configure<IConfiguration>((o, c) => c.GetSection("Geocoding").Bind(o));
        services.AddHttpClient("geocoding", c => c.Timeout = TimeSpan.FromSeconds(6)).RemoveAllLoggers();
        services.AddSingleton<Nexo.Discovery.Application.IPlaceGeocoder>(sp => new PhotonGeocoder(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("geocoding"), sp.GetRequiredService<IOptions<GeocodingOptions>>()));
        services.AddSingleton<MongoProviderStore>();
        services.AddSingleton<IProviderStore>(sp => sp.GetRequiredService<MongoProviderStore>());
        services.AddSingleton<IDirectoryProfiles>(sp => sp.GetRequiredService<MongoProviderStore>());
        services.AddSingleton<IDirectoryCatalog, MongoDirectoryCatalog>();
        services.AddSingleton<IProviderSortStrategy, RelevantProviderSort>();
        services.AddSingleton<IProviderSortStrategy, NearestProviderSort>();
        services.AddSingleton<IProviderSortStrategy, RecommendedProviderSort>();
        services.AddScoped<DirectoryService>();
        services.AddScoped<ProviderService>();
        services.AddSingleton<Nexo.Discovery.Application.Works.IWorkStore, Nexo.Discovery.Infrastructure.Works.MongoWorkStore>();
        services.AddSingleton<Nexo.Discovery.Application.Works.IFeedStore, Nexo.Discovery.Infrastructure.Works.MongoFeedStore>();
        services.AddScoped<Nexo.Discovery.Application.Works.FeedService>();
        services.AddScoped<Nexo.Discovery.Application.Works.WorkService>();
        services.AddSingleton<Nexo.Discovery.Application.Saved.ISavedStore, Nexo.Discovery.Infrastructure.Saved.MongoSavedStore>();
        services.AddScoped<Nexo.Discovery.Application.Saved.SavedService>();
        services.AddSingleton<Nexo.Discovery.Application.Requests.IRequestStore, Nexo.Discovery.Infrastructure.Requests.MongoRequestStore>();
        services.AddScoped<Nexo.Discovery.Application.Requests.RequestService>();
        services.AddOptions<ProviderSafetyOptions>().Configure<IConfiguration>((options, configuration) => configuration.GetSection("ContentSafety").Bind(options));
        services.AddHttpClient<IProviderSafety, ProviderSafety>(client => client.Timeout = TimeSpan.FromSeconds(8));
        services.AddOptions<Nexo.Discovery.Infrastructure.Media.MediaOptions>().Configure<IConfiguration>((o,c)=>c.GetSection("MediaStorage").Bind(o));
        services.AddSingleton<Nexo.Discovery.Infrastructure.Media.AzureMediaStorage>();
        services.AddSingleton<Nexo.Discovery.Infrastructure.Media.IMediaStorage>(sp=>sp.GetRequiredService<Nexo.Discovery.Infrastructure.Media.AzureMediaStorage>());
        services.AddHttpClient<Nexo.Discovery.Infrastructure.Media.IImageModeration,Nexo.Discovery.Infrastructure.Media.ImageModeration>(c=>c.Timeout=TimeSpan.FromSeconds(20)).RemoveAllLoggers();
        services.AddScoped<Nexo.Discovery.Infrastructure.Media.MongoMediaService>();
        services.AddScoped<Nexo.Discovery.Application.Media.IMediaService>(sp=>sp.GetRequiredService<Nexo.Discovery.Infrastructure.Media.MongoMediaService>());
        services.AddScoped<Nexo.Discovery.Contracts.IMediaContract>(sp=>sp.GetRequiredService<Nexo.Discovery.Infrastructure.Media.MongoMediaService>());
        // Permite ejecutar la API local contra medios existentes sin consumir la cola
        // compartida del almacenamiento configurado. En producción permanece habilitado.
        if (!configuration.GetValue<bool>("MediaStorage:DisableWorker"))
            services.AddHostedService<Nexo.Discovery.Infrastructure.Media.MediaWorker>();
        return services;
    }
}

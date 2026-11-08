using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Discovery.Infrastructure;

public static class DiscoveryInfrastructureRegistration
{
    /// <summary>
    /// Adaptadores de Discovery; recibe la base keyed y nunca un cliente global.
    /// Vacio a proposito: el feed anterior se retiro por estar mal elegido.
    /// </summary>
    public static IServiceCollection AddDiscoveryInfrastructure(this IServiceCollection services) => services;
}

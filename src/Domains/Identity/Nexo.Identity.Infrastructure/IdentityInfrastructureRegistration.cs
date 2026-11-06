using Microsoft.Extensions.DependencyInjection;
using Nexo.Identity.Infrastructure.Persistence;

namespace Nexo.Identity.Infrastructure;

public static class IdentityInfrastructureRegistration
{
    /// <summary>Adaptadores de Identity; recibe la base keyed y nunca un cliente global.</summary>
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddIdentityMongo();
        services.AddIdentityFeatures();
        return services;
    }
}

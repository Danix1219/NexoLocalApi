using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Engagement.Infrastructure;

public static class EngagementInfrastructureRegistration
{
    /// <summary>Adaptadores de Engagement; recibe la base keyed y nunca un cliente global.</summary>
    public static IServiceCollection AddEngagementInfrastructure(this IServiceCollection services) => services;
}

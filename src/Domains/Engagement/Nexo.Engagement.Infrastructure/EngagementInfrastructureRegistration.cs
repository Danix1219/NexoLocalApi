using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Engagement.Infrastructure;

public static class EngagementInfrastructureRegistration
{
    /// <summary>Adaptadores de Engagement; recibe la base keyed y nunca un cliente global.</summary>
    public static IServiceCollection AddEngagementInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<Nexo.Engagement.Contracts.IInterestSummaryContract, InterestSummaryContract>();
        services.AddSingleton<Nexo.Engagement.Application.Interests.IInterestStore, MongoInterestStore>();
        services.AddScoped<Nexo.Engagement.Application.Interests.InterestService>();
        services.AddSingleton<Nexo.Engagement.Application.Orders.IOrderStore, MongoOrderStore>();
        services.AddSingleton<IOrderAddressKeyWrapper, AzureOrderAddressKeyWrapper>();
        services.AddSingleton<Nexo.Engagement.Application.Orders.IOrderAddressProtection, OrderAddressProtection>();
        services.AddScoped<Nexo.Engagement.Application.Orders.OrderService>();
        return services;
    }
}

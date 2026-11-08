using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Common.Security;

namespace Nexo.Common;

public static class DependencyInjection
{
    /// <summary>Servicios transversales minimos; cada dominio aporta sus adaptadores.</summary>
    public static IServiceCollection AddCommon(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }
}

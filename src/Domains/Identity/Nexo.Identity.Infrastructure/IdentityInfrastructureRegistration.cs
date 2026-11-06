using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Identity.Infrastructure;

public static class IdentityInfrastructureRegistration
{
    /// <summary>
    /// Adaptadores de Identity; recibe la base keyed y nunca un cliente global.
    /// Vacio a proposito: las operaciones anteriores se retiraron por estar mal elegidas
    /// y se registraran de nuevo al definir las correctas.
    /// </summary>
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services) => services;
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Common.Redis;

namespace Nexo.Integration;

/// <summary>
/// nv-00 AC-006 y requisito 10: con dos instancias de App Service, un logout en una debe
/// surtir efecto en la otra. Cada ServiceProvider simula una instancia: procesos distintos,
/// memoria distinta, el mismo Redis.
/// </summary>
[Collection("infra")]
[Trait("Category", "Integration")]
public sealed class RevocacionEntreInstanciasTests(InfraFixture infra)
{
    private ServiceProvider Instancia()
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = infra.RedisConnectionString
        }).Build();

        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddSharedRedis(configuracion);
        return servicios.BuildServiceProvider();
    }

    [Fact]
    public async Task Un_jti_revocado_en_A_es_rechazado_por_B()
    {
        using var a = Instancia();
        using var b = Instancia();
        var sesion = Guid.CreateVersion7().ToString();

        var revocacionA = a.GetRequiredService<ISessionRevocationStore>();
        var revocacionB = b.GetRequiredService<ISessionRevocationStore>();

        // Antes del logout, B considera la sesion valida.
        Assert.False(await revocacionB.IsRevokedAsync(sesion));

        await revocacionA.RevokeAsync(sesion, TimeSpan.FromMinutes(15));

        // En la peticion siguiente, B ya la rechaza: esto es lo que no puede vivir en memoria.
        Assert.True(await revocacionB.IsRevokedAsync(sesion));
    }

    [Fact]
    public async Task La_revocacion_no_alcanza_a_otras_sesiones()
    {
        using var a = Instancia();
        using var b = Instancia();
        var revocada = Guid.CreateVersion7().ToString();
        var intacta = Guid.CreateVersion7().ToString();

        await a.GetRequiredService<ISessionRevocationStore>().RevokeAsync(revocada, TimeSpan.FromMinutes(15));

        var revocacionB = b.GetRequiredService<ISessionRevocationStore>();
        Assert.True(await revocacionB.IsRevokedAsync(revocada));
        Assert.False(await revocacionB.IsRevokedAsync(intacta));
    }

    [Fact]
    public async Task El_contador_de_rate_limit_es_compartido_por_las_dos_instancias()
    {
        // NFR-003: si cada instancia contara por su cuenta, el techo real seria el doble.
        using var a = Instancia();
        using var b = Instancia();
        var particion = Guid.CreateVersion7().ToString();
        var ventana = TimeSpan.FromMinutes(1);

        Assert.True(await a.GetRequiredService<SharedRateLimiter>().IsAllowedAsync(particion, 2, ventana));
        Assert.True(await b.GetRequiredService<SharedRateLimiter>().IsAllowedAsync(particion, 2, ventana));

        // El tercero excede el techo de 2 aunque llegue a la otra instancia.
        Assert.False(await b.GetRequiredService<SharedRateLimiter>().IsAllowedAsync(particion, 2, ventana));
        Assert.False(await a.GetRequiredService<SharedRateLimiter>().IsAllowedAsync(particion, 2, ventana));
    }

    [Fact]
    public async Task La_idempotencia_cubre_un_reintento_que_cae_en_la_otra_instancia()
    {
        using var a = Instancia();
        using var b = Instancia();
        var clave = Guid.CreateVersion7().ToString();

        var primera = await a.GetRequiredService<IIdempotencyStore>().TryReserveAsync(clave, TimeSpan.FromMinutes(5));
        var reintento = await b.GetRequiredService<IIdempotencyStore>().TryReserveAsync(clave, TimeSpan.FromMinutes(5));

        Assert.True(primera);
        Assert.False(reintento);
    }
}

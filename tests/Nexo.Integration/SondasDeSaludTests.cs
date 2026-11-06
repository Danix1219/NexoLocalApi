using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Nexo.Integration;

/// <summary>
/// nv-00 AC-007: la readiness refleja las dependencias. Si Redis o Mongo no responden,
/// la instancia debe salir de rotacion con 503, no seguir recibiendo trafico con 200.
/// </summary>
[Collection("infra")]
[Trait("Category", "Integration")]
public sealed class SondasDeSaludTests(InfraFixture infra)
{
    private sealed class Host(string mongo, string redis) : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                
                ["Mongo:ConnectionString"] = mongo,
                ["Redis:ConnectionString"] = redis
            }));
            return base.CreateHost(builder);
        }
    }

    [Fact]
    public async Task Liveness_responde_200_sin_consultar_dependencias()
    {
        // /health/live dice "el proceso vive". Si dependiera de Mongo o Redis, una caida
        // de Redis haria que App Service reiniciara instancias sanas en bucle.
        using var host = new Host("mongodb://127.0.0.1:1/", "127.0.0.1:1,connectTimeout=200,abortConnect=false");
        using var cliente = host.CreateClient();

        var respuesta = await cliente.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Readiness_responde_200_con_Mongo_y_Redis_arriba()
    {
        using var host = new Host(infra.MongoConnectionString, infra.RedisConnectionString);
        using var cliente = host.CreateClient();

        var respuesta = await cliente.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Readiness_responde_503_con_Redis_caido()
    {
        // El caso negativo que exige el checklist: 503, y no 200 por omision.
        using var host = new Host(infra.MongoConnectionString, "127.0.0.1:1,connectTimeout=200,abortConnect=false");
        using var cliente = host.CreateClient();

        var respuesta = await cliente.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
    }

    [Fact]
    public async Task Readiness_responde_503_con_Mongo_caido()
    {
        using var host = new Host("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500", infra.RedisConnectionString);
        using var cliente = host.CreateClient();

        var respuesta = await cliente.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
    }
}

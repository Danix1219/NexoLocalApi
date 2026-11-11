using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Engagement.Contracts;
using Nexo.Engagement.Infrastructure;
using Nexo.Host;
using Nexo.Identity.Infrastructure.Persistence;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class RequestContractsTests(InfraFixture infra)
{
    [Fact]
    public async Task Resumen_real_cuenta_omite_retirados_limita_preview_y_marca_fuera_del_preview()
    {
        var db = new MongoClient(infra.MongoConnectionString).GetDatabase("interest_summary_" + Guid.NewGuid().ToString("N"));
        var request = Guid.NewGuid(); var providers = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        for (var i = 0; i < 6; i++)
        {
            var row = new BsonDocument { ["requestId"] = new BsonBinaryData(request, GuidRepresentation.Standard), ["providerId"] = new BsonBinaryData(providers[i], GuidRepresentation.Standard), ["createdAt"] = DateTime.UtcNow.AddMinutes(-i) };
            if (i == 5) row["withdrawnAt"] = DateTime.UtcNow;
            await db.GetCollection<BsonDocument>("interests").InsertOneAsync(row);
        }
        var contract = new InterestSummaryContract(db);
        var missing = Guid.NewGuid(); var results = await contract.ReadAsync([request, missing], providers[4], default);
        Assert.Equal(5, results[request].Count); Assert.Equal(4, results[request].ProviderIds.Length); Assert.True(results[request].Interested);
        Assert.DoesNotContain(providers[4], results[request].ProviderIds); Assert.Equal(0, results[missing].Count);
        Assert.DoesNotContain("clientId", System.Text.Json.JsonSerializer.Serialize(results));
    }
    private sealed class Broken : IInterestSummaryContract
    {
        public int Calls;
        public Task<IReadOnlyDictionary<Guid, InterestSummary>> ReadAsync(Guid[] ids, Guid? viewer, CancellationToken ct)
        { Calls++; throw new TimeoutException(); }
    }
    [Fact]
    public async Task Circuito_de_resumen_degrada_y_respeta_cancelacion_sin_falsear_un_exito()
    {
        var source = new Broken(); var bridge = new RequestInterestsBridge(source, TimeProvider.System);
        for (var n = 0; n < 4; n++) Assert.False((await bridge.ReadAsync([Guid.NewGuid()], null, default)).Available);
        Assert.Equal(3, source.Calls);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bridge.ReadAsync([], null, cts.Token));
    }
    [Fact]
    public void Nombre_publico_de_solicitud_muestra_nombre_completo()
    {
        Assert.Equal("Daniel Martínez Pérez", RequestClientContract.DisplayName(" Daniel Martínez Pérez "));
        Assert.Equal("Ángela Íñiguez", RequestClientContract.DisplayName("Ángela Íñiguez"));
    }
}

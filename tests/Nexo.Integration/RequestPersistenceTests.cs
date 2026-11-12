using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Discovery.Application.Requests;
using Nexo.Discovery.Domain;
using Nexo.Discovery.Infrastructure.Requests;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class RequestPersistenceTests(InfraFixture infra)
{
    private (IMongoDatabase Db, MongoRequestStore Store) Setup()
    {
        var db = new MongoClient(infra.MongoConnectionString).GetDatabase("requests_" + Guid.NewGuid().ToString("N"));
        return (db, new MongoRequestStore(db));
    }
    private static readonly ProviderPoint Exact = new(20.05, -99.34);
    private static ServiceRequest Request(string category = "plomeria", string urgency = "today") => new(Guid.CreateVersion7(), category, "Fuga en el baño",
        "Reparación urgente de una tubería debajo del lavabo.", urgency, "chat", Exact.Blur(), "Zona aproximada", null, "open", 0, 2, DateTime.UtcNow, DateTime.UtcNow);
    [Fact]
    public async Task Alta_concurrente_reintento_cuota_y_evento_son_atomicos()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var key = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => s.Store.CreateAsync(owner, key, "same", Request(), Exact, default)));
        Assert.All(results, r => Assert.True(r.IsSuccess)); Assert.Single(results.Select(r => r.Value!.RequestId).Distinct());
        var quota = await s.Db.GetCollection<BsonDocument>("request_daily_quotas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync(); Assert.Equal(1, quota["count"].ToInt32());
        Assert.Equal(1, await s.Db.GetCollection<BsonDocument>("discovery_events").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(NexoCodes.RequestIdempotencyKeyReused, (await s.Store.CreateAsync(owner, key, "changed", Request(), Exact, default)).Error!.Code);
        for (var i = 1; i < 10; i++) Assert.True((await s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", Request(), Exact, default)).IsSuccess);
        Assert.Equal(NexoCodes.RateLimitExceeded, (await s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", Request(), Exact, default)).Error!.Code);
        Assert.True((await s.Store.CreateAsync(owner, key, "same", Request(), Exact, default)).IsSuccess);
        Assert.True((await s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", Request() with { CreatedAt = DateTime.UtcNow.AddDays(1) }, Exact, default)).IsSuccess);
    }
    [Fact]
    public async Task Punto_exacto_solo_en_coleccion_privada_proyecciones_recibos_y_eventos_no_lo_exponen()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var r = Request();
        Assert.True((await s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", r, Exact, default)).IsSuccess);
        var doc = await s.Db.GetCollection<BsonDocument>("service_requests").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.Equal(Exact.Lng, doc["location"]["coordinates"][0].ToDouble()); Assert.Equal(Exact.Lat, doc["location"]["coordinates"][1].ToDouble());
        Assert.False(doc["data"].AsBsonDocument.Contains("location"));
        var own = Assert.Single(await s.Store.MineAsync(owner, "all", null, 20, default)); Assert.Equal(r.PublicPoint, own.PublicPoint);
        Assert.Empty(await s.Store.MineAsync(Guid.NewGuid(), "all", null, 20, default)); Assert.Null(await s.Store.OwnAsync(Guid.NewGuid(), r.RequestId, default));
        var candidate = Assert.Single(await s.Store.CandidatesAsync(Guid.NewGuid(), Exact, 5, ["plomeria"], "REPARACION tuberia", true, default));
        Assert.Equal(r.PublicPoint, candidate.Request.PublicPoint); Assert.InRange(candidate.DistanceKm, 0, .001);
        foreach (var collection in new[] { "request_operations", "discovery_events" })
        {
            var value = await s.Db.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
            Assert.False(value.Contains("location"));
            if (value.Contains("publicPoint")) Assert.InRange(Exact.DistanceKm(new(value["publicPoint"]["lat"].ToDouble(), value["publicPoint"]["lng"].ToDouble())), .499, .801);
        }
    }
    [Fact]
    public async Task Cierre_concurrente_solo_dueno_un_evento_y_fecha_original_sin_reabrir()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var request = Request(); var key = Guid.NewGuid();
        Assert.True((await s.Store.CreateAsync(owner, key, "hash", request, Exact, default)).IsSuccess);
        Assert.Equal(NexoCodes.RequestNotFound, (await s.Store.CloseAsync(Guid.NewGuid(), request.RequestId, "noLongerNeeded", null, DateTime.UtcNow, default)).Error!.Code);
        var closed = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => s.Store.CloseAsync(owner, request.RequestId, "solvedHere", Guid.NewGuid(), DateTime.UtcNow, default)));
        Assert.All(closed, c => Assert.True(c.IsSuccess)); Assert.Single(closed, c => !c.Value!.AlreadyClosed);
        Assert.Single(closed.Select(c => c.Value!.Request.ClosedAt).Distinct());
        Assert.Equal(1, await s.Db.GetCollection<BsonDocument>("discovery_events").CountDocumentsAsync(new BsonDocument("type", "request.closed")));
        Assert.Empty(await s.Store.CandidatesAsync(Guid.NewGuid(), Exact, 10, null, "", false, default));
        Assert.Single(await s.Store.MineAsync(owner, "closed", null, 20, default));
        Assert.Equal("closed", (await s.Store.CreateAsync(owner, key, "hash", request, Exact, default)).Value!.Status);
    }
    [Fact]
    public async Task Filtros_servidor_excluyen_propias_categoria_radio_estado_y_texto_no_es_regex()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var viewer = Guid.NewGuid();
        var cases = new[] { (owner, Request()), (viewer, Request()), (owner, Request("pintura")), (owner, Request(urgency: "week")) };
        foreach (var (user, r) in cases) Assert.True((await s.Store.CreateAsync(user, Guid.NewGuid(), "hash", r, Exact, default)).IsSuccess);
        var far = Request(); await s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", far, new(21.05, -99.34), default);
        Assert.Single(await s.Store.CandidatesAsync(viewer, Exact, 5, ["plomeria"], "reparacion urgente", true, default));
        Assert.Empty(await s.Store.CandidatesAsync(viewer, Exact, 5, ["plomeria"], ".*", false, default));
        Assert.Equal(3, (await s.Store.CandidatesAsync(viewer, Exact, 5, null, "", false, default)).Count);
        Assert.Equal(4, (await s.Store.CandidatesAsync(viewer, Exact, null, null, "", false, default)).Count);
        await s.Db.GetCollection<BsonDocument>("service_requests").UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, new BsonDocument("$set", new BsonDocument("moderation.status", "rejected")));
        Assert.Empty(await s.Store.CandidatesAsync(viewer, Exact, null, null, "", false, default));
    }
    [Fact]
    public async Task Fallo_evento_revierte_alta_y_cierre_y_cursores_no_necesitan_ancla_viva()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var request = Request();
        await s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", request, Exact, default);
        var another = Request(); await s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", another, Exact, default);
        var first = (await s.Store.MineAsync(owner, "all", null, 1, default))[0];
        var next = await s.Store.MineAsync(owner, "all", first.RequestId, 20, default); Assert.Single(next); Assert.NotEqual(first.RequestId, next[0].RequestId);
        await s.Db.RunCommandAsync<BsonDocument>(new BsonDocument { ["collMod"] = "discovery_events", ["validator"] = new BsonDocument("mustNotExist", true), ["validationLevel"] = "strict" });
        await Assert.ThrowsAsync<RequestUnavailableException>(() => s.Store.CreateAsync(owner, Guid.NewGuid(), "hash", Request(), Exact, default));
        await Assert.ThrowsAsync<RequestUnavailableException>(() => s.Store.CloseAsync(owner, request.RequestId, "noLongerNeeded", null, DateTime.UtcNow, default));
        Assert.Equal("open", (await s.Store.OwnAsync(owner, request.RequestId, default))!.Status);
        Assert.Equal(2, await s.Db.GetCollection<BsonDocument>("request_operations").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(2, (await s.Db.GetCollection<BsonDocument>("request_daily_quotas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync())["count"].ToInt32());
    }
}

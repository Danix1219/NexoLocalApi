using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Works;
using Nexo.Discovery.Domain;
using Nexo.Discovery.Infrastructure.Providers;
using Nexo.Discovery.Infrastructure.Works;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class WorkPersistenceTests(InfraFixture infra)
{
    private async Task<(IMongoDatabase Db, MongoProviderStore Profiles, MongoWorkStore Works, MongoFeedStore Feed, Guid Owner, Guid Provider)> Setup()
    {
        var db = new MongoClient(infra.MongoConnectionString).GetDatabase("works_" + Guid.NewGuid().ToString("N"));
        var profiles = new MongoProviderStore(db); var works = new MongoWorkStore(db); var feed = new MongoFeedStore(db);
        var owner = Guid.NewGuid(); var provider = Guid.CreateVersion7();
        Assert.True(await profiles.InsertAsync(new(owner, new() { ProviderId = provider, Name = "Prestador de prueba", Categories = ["plomeria"],
            Location = new(20.05, -99.34), UpdatedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }, 0, "key", "hash", true), default));
        return (db, profiles, works, feed, owner, provider);
    }
    private static Work Work(Guid provider) => new(Guid.CreateVersion7(), provider, "plomeria", "Trabajo de prueba", "Reparación terminada con todos los detalles necesarios.",
        null, null, null, null, new ProviderPoint(20.05, -99.34).Blur(), "Zona aproximada", [], 0, DateTime.UtcNow, DateTime.UtcNow);
    [Fact]
    public async Task Alta_concurrente_misma_clave_es_atomica_en_trabajo_cuota_perfil_y_evento()
    {
        var s = await Setup(); var key = Guid.NewGuid();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => s.Works.CreateAsync(s.Owner, key, "same-hash", Work(s.Provider), default)));
        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Single(results.Select(r => r.Value!.WorkId).Distinct());
        Assert.Equal(1, await s.Db.GetCollection<BsonDocument>("works").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(1, (await s.Profiles.OwnAsync(s.Owner, default))!.Profile.Jobs);
        var quota = await s.Db.GetCollection<BsonDocument>("work_daily_quotas").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.Equal(1, quota["count"].ToInt32());
        Assert.Equal(1, await s.Db.GetCollection<BsonDocument>("discovery_events").CountDocumentsAsync(new BsonDocument("type", "work.created")));
        Assert.Equal(NexoCodes.RequestIdempotencyKeyReused, (await s.Works.CreateAsync(s.Owner, key, "different-hash", Work(s.Provider), default)).Error!.Code);
    }
    [Fact]
    public async Task Recomendaciones_concurrentes_son_unicas_y_borrado_limpia_contadores_y_relaciones()
    {
        var s = await Setup(); var work = Work(s.Provider); var key = Guid.NewGuid();
        Assert.True((await s.Works.CreateAsync(s.Owner, key, "hash", work, default)).IsSuccess);
        var user = Guid.NewGuid();
        var first = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => s.Works.RecommendAsync(user, work.WorkId, true, default)));
        Assert.All(first, r => { Assert.True(r.IsSuccess); Assert.Equal(1, r.Value!.Recs); });
        Assert.Equal(1, (await s.Profiles.OwnAsync(s.Owner, default))!.Profile.Recs);
        var removals = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => s.Works.RecommendAsync(user, work.WorkId, false, default)));
        Assert.All(removals, r => { Assert.True(r.IsSuccess); Assert.Equal(0, r.Value!.Recs); });
        Assert.Equal(0, (await s.Profiles.OwnAsync(s.Owner, default))!.Profile.Recs);
        Assert.Equal(NexoCodes.WorkOwnContent, (await s.Works.RecommendAsync(s.Owner, work.WorkId, true, default)).Error!.Code);
        Assert.Equal(NexoCodes.WorkNotFound, (await s.Works.DeleteAsync(Guid.NewGuid(), work.WorkId, default)).Error!.Code);
        await s.Works.RecommendAsync(user, work.WorkId, true, default);
        await s.Works.RecommendAsync(Guid.NewGuid(), work.WorkId, true, default);
        await s.Db.GetCollection<BsonDocument>("saved").InsertOneAsync(new BsonDocument { ["accountId"] = new BsonBinaryData(user, GuidRepresentation.Standard),
            ["kind"] = "work", ["targetId"] = new BsonBinaryData(work.WorkId, GuidRepresentation.Standard) });
        Assert.True((await s.Works.DeleteAsync(s.Owner, work.WorkId, default)).IsSuccess);
        Assert.Null(await s.Works.FindAsync(work.WorkId, default));
        Assert.Empty(await s.Db.GetCollection<BsonDocument>("recommendations").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        Assert.Empty(await s.Db.GetCollection<BsonDocument>("saved").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        var profile = (await s.Profiles.OwnAsync(s.Owner, default))!.Profile;
        Assert.Equal(0, profile.Jobs); Assert.Equal(0, profile.Recs);
        Assert.Equal(NexoCodes.WorkNotFound, (await s.Works.CreateAsync(s.Owner, key, "hash", Work(s.Provider), default)).Error!.Code);
        var receipt = await s.Db.GetCollection<BsonDocument>("work_operations").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.False(receipt.Contains("data")); Assert.False(receipt.Contains("location"));
    }
    [Fact]
    public async Task Cuota_durable_30_por_dia_no_consume_por_reintento_y_rechaza_31()
    {
        var s = await Setup(); var work = Work(s.Provider); var key = Guid.NewGuid();
        for (var n = 0; n < 30; n++) Assert.True((await s.Works.CreateAsync(s.Owner, n == 0 ? key : Guid.NewGuid(), "hash", n == 0 ? work : Work(s.Provider), default)).IsSuccess);
        Assert.True((await s.Works.CreateAsync(s.Owner, key, "hash", Work(s.Provider), default)).IsSuccess);
        Assert.Equal(NexoCodes.RateLimitExceeded, (await s.Works.CreateAsync(s.Owner, Guid.NewGuid(), "hash", Work(s.Provider), default)).Error!.Code);
        Assert.Equal(30, (await s.Profiles.OwnAsync(s.Owner, default))!.Profile.Jobs);
        var tomorrow = Work(s.Provider) with { CreatedAt = DateTime.UtcNow.AddDays(1) };
        Assert.True((await s.Works.CreateAsync(s.Owner, Guid.NewGuid(), "hash", tomorrow, default)).IsSuccess);
    }
    [Fact]
    public async Task Fallo_al_emitir_evento_revierte_todas_las_escrituras_de_la_transaccion()
    {
        var s = await Setup(); await s.Works.FindAsync(Guid.NewGuid(), default); // indexes/collections outside transaction.
        await s.Db.RunCommandAsync<BsonDocument>(new BsonDocument { ["collMod"] = "discovery_events", ["validator"] = new BsonDocument("mustNotExist", true), ["validationLevel"] = "strict" });
        await Assert.ThrowsAsync<WorkUnavailableException>(() => s.Works.CreateAsync(s.Owner, Guid.NewGuid(), "hash", Work(s.Provider), default));
        foreach (var name in new[] { "works", "work_operations", "work_daily_quotas", "discovery_events" })
            Assert.Equal(0, await s.Db.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(0, (await s.Profiles.OwnAsync(s.Owner, default))!.Profile.Jobs);
    }
    [Fact]
    public async Task Paginar_marcas_privadas_y_documentos_sin_punto_exacto()
    {
        var s = await Setup(); var ids = new List<Guid>();
        for (var i = 0; i < 5; i++) { var w = Work(s.Provider); ids.Add(w.WorkId); Assert.True((await s.Works.CreateAsync(s.Owner, Guid.NewGuid(), "hash", w, default)).IsSuccess); }
        var first = await s.Works.ListAsync(s.Provider, null, 2, null, default);
        Assert.True(first.HasMore); Assert.Empty(first.Recommended);
        var second = await s.Works.ListAsync(s.Provider, first.Items[^1].WorkId, 2, null, default);
        Assert.Empty(first.Items.Select(w => w.WorkId).Intersect(second.Items.Select(w => w.WorkId)));
        var viewer = Guid.NewGuid(); await s.Works.RecommendAsync(viewer, first.Items[0].WorkId, true, default);
        Assert.Single((await s.Works.ListAsync(s.Provider, null, 2, viewer, default)).Recommended);
        Assert.Empty((await s.Works.ListAsync(s.Provider, null, 2, Guid.NewGuid(), default)).Recommended);
        var document = await s.Db.GetCollection<BsonDocument>("works").Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();
        Assert.False(document.Contains("location")); Assert.False(document["data"].AsBsonDocument.Contains("location"));
        var record = (await s.Works.FindAsync(Guid.Parse(document["_id"].AsString), default))!;
        Assert.InRange(new ProviderPoint(20.05, -99.34).DistanceKm(record.PublicPoint), .499, .801);
        var point = document["publicGeo"]["coordinates"].AsBsonArray;
        Assert.Equal(record.PublicPoint.Lng, point[0].ToDouble()); Assert.Equal(record.PublicPoint.Lat, point[1].ToDouble());
    }
    [Fact]
    public async Task Trabajo_retirado_por_moderacion_no_se_lista_pero_el_autor_puede_borrarlo()
    {
        var s = await Setup(); var work = Work(s.Provider);
        Assert.True((await s.Works.CreateAsync(s.Owner, Guid.NewGuid(), "hash", work, default)).IsSuccess);
        await s.Db.GetCollection<BsonDocument>("works").UpdateOneAsync(new BsonDocument("_id", work.WorkId.ToString()),
            new BsonDocument("$set", new BsonDocument("moderation.status", "rejected")));
        Assert.Null(await s.Works.FindAsync(work.WorkId, default));
        Assert.Empty((await s.Works.ListAsync(s.Provider, null, 10, s.Owner, default)).Items);
        Assert.True((await s.Works.DeleteAsync(s.Owner, work.WorkId, default)).IsSuccess);
        Assert.Equal(0, (await s.Profiles.OwnAsync(s.Owner, default))!.Profile.Jobs);
    }

    [Fact]
    public async Task Feed_combina_trabajos_por_cobertura_y_cercania_pagina_y_marca_estado_personal()
    {
        var s = await Setup();
        var first = Work(s.Provider);
        var second = Work(s.Provider);
        Assert.True((await s.Works.CreateAsync(s.Owner, Guid.NewGuid(), "feed-1", first, default)).IsSuccess);
        Assert.True((await s.Works.CreateAsync(s.Owner, Guid.NewGuid(), "feed-2", second, default)).IsSuccess);
        var viewer = Guid.NewGuid();
        var center = new ProviderPoint(20.0539, -99.3403);
        var page1 = await s.Feed.ReadAsync(center, [s.Provider], null, 1, viewer, default);
        Assert.Single(page1.Items);
        Assert.True(page1.HasMore);
        var last = page1.Items[0];

        // Feed ordering is based on persisted IDs and can place either insert first.
        // Mark the actual first-page item instead of assuming which generated UUID sorts first.
        Assert.True((await s.Works.RecommendAsync(viewer, last.WorkId, true, default)).IsSuccess);
        await s.Db.GetCollection<BsonDocument>("saved").InsertOneAsync(new BsonDocument
        {
            ["accountId"] = new BsonBinaryData(viewer, GuidRepresentation.Standard),
            ["kind"] = "work",
            ["targetId"] = new BsonBinaryData(last.WorkId, GuidRepresentation.Standard)
        });
        var markedPage1 = await s.Feed.ReadAsync(center, [s.Provider], null, 1, viewer, default);
        Assert.Contains(last.WorkId, markedPage1.Recommended);
        Assert.Contains(last.WorkId, markedPage1.Saved);

        var page2 = await s.Feed.ReadAsync(center, [s.Provider], markedPage1.Items[0].WorkId, 1, viewer, default);
        Assert.Single(page2.Items);
        Assert.False(page2.HasMore);
        Assert.NotEqual(markedPage1.Items[0].WorkId, page2.Items[0].WorkId);
        Assert.InRange(center.DistanceKm(markedPage1.Items[0].PublicPoint), 0, 3.8);
    }
}

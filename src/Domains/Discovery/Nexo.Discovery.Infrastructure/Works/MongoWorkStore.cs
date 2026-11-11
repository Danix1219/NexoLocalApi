using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Common.Mongo;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Works;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Infrastructure.Works;

public sealed class MongoWorkStore([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : IWorkStore
{
    private readonly IMongoCollection<BsonDocument> works = database.GetCollection<BsonDocument>("works");
    private readonly IMongoCollection<BsonDocument> recommendations = database.GetCollection<BsonDocument>("recommendations");
    private readonly IMongoCollection<BsonDocument> profiles = database.GetCollection<BsonDocument>("provider_profiles");
    private readonly IMongoCollection<BsonDocument> receipts = database.GetCollection<BsonDocument>("work_operations");
    private readonly IMongoCollection<BsonDocument> quotas = database.GetCollection<BsonDocument>("work_daily_quotas");
    private readonly IMongoCollection<BsonDocument> events = database.GetCollection<BsonDocument>("discovery_events");
    private readonly IMongoCollection<BsonDocument> saved = database.GetCollection<BsonDocument>("saved");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private static BsonBinaryData Id(Guid id) => new(id, GuidRepresentation.Standard);
    private static string Key(Guid owner, Guid key) => owner.ToString("N") + ":" + key.ToString("N");
    private static BsonDocument Approved(Guid id) => new() { ["_id"] = id.ToString("D"), ["moderation.status"] = "approved" };
    private static Work Read(BsonDocument document) => JsonSerializer.Deserialize<Work>(document["data"].AsBsonDocument.ToJson(
        new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }), ProviderService.Json)!;
    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await works.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("providerId").Descending("_id"), new CreateIndexOptions { Name = "ix_work_provider_cursor" }),
                new(Builders<BsonDocument>.IndexKeys.Geo2DSphere("publicGeo").Descending("createdAt"), new CreateIndexOptions { Name = "ix_work_public_geo" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("category").Descending("createdAt"), new CreateIndexOptions { Name = "ix_work_category" })
            ], ct);
            await recommendations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("accountId").Ascending("workId"),
                new CreateIndexOptions { Unique = true, Name = "ux_recommendation_account_work" }), cancellationToken: ct);
            await recommendations.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("workId"), new CreateIndexOptions { Name = "ix_recommendation_work" }), cancellationToken: ct);
            // Create these collections/indexes outside transactions (and before concurrent requests).
            foreach (var collection in new[] { receipts, quotas, events, saved })
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("createdAt"), new CreateIndexOptions { Name = "ix_created" }), cancellationToken: ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }
    private async Task<T> QueryAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try { await EnsureAsync(ct); return await action(); }
        catch (Exception e) when (e is MongoException or TimeoutException or NotSupportedException) { throw new WorkUnavailableException(); }
    }
    private async Task<Result<T>> TransactionAsync<T>(Func<IClientSessionHandle, CancellationToken, Task<Result<T>>> action, CancellationToken ct)
    {
        return await QueryAsync(async () =>
        {
            // Unique upserts may race before either transaction commits; retry from a new snapshot.
            for (var attempt = 0; attempt < 4; attempt++)
            {
                using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
                try
                {
                    return await session.WithTransactionAsync(action,
                        new TransactionOptions(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority), ct);
                }
                catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey && attempt < 3) { }
                catch (MongoCommandException e) when (e.Code == 11000 && attempt < 3) { }
            }
            throw new WorkUnavailableException();
        }, ct);
    }
    public Task<WorkReceipt?> ReceiptAsync(Guid owner, Guid key, CancellationToken ct) => QueryAsync(async () =>
    {
        var doc = await receipts.Find(new BsonDocument("_id", Key(owner, key))).FirstOrDefaultAsync(ct);
        return doc == null ? null : new WorkReceipt(doc["hash"].AsString, Guid.Parse(doc["workId"].AsString));
    }, ct);
    public Task<Work?> FindAsync(Guid id, CancellationToken ct) => QueryAsync(async () =>
    {
        var doc = await works.Find(Approved(id)).FirstOrDefaultAsync(ct);
        return doc == null ? null : Read(doc);
    }, ct);
    public Task<Result<Work>> CreateAsync(Guid owner, Guid key, string hash, Work work, CancellationToken ct) => TransactionAsync(async (session, token) =>
    {
        var receipt = await receipts.Find(session, new BsonDocument("_id", Key(owner, key))).FirstOrDefaultAsync(token);
        if (receipt != null)
        {
            if (receipt["hash"] != hash) return Fail<Work>(NexoCodes.RequestIdempotencyKeyReused);
            var old = await works.Find(session, Approved(Guid.Parse(receipt["workId"].AsString))).FirstOrDefaultAsync(token);
            return old == null ? Fail<Work>(NexoCodes.WorkNotFound) : Result<Work>.Success(Read(old));
        }
        var providerFilter = new BsonDocument { ["_id"] = Id(work.ProviderId), ["accountId"] = Id(owner), ["published"] = true, ["data.categories"] = work.Category };
        if (!await profiles.Find(session, providerFilter).AnyAsync(token)) return Fail<Work>(NexoCodes.WorkCategoryNotInProfile);
        var quotaId = owner.ToString("N") + ":" + work.CreatedAt.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var quota = await quotas.Find(session, new BsonDocument("_id", quotaId)).FirstOrDefaultAsync(token);
        if (quota?.GetValue("count", 0).ToInt32() >= 30) return Fail<Work>(NexoCodes.RateLimitExceeded);
        await quotas.UpdateOneAsync(session, new BsonDocument("_id", quotaId), new BsonDocument
        {
            ["$inc"] = new BsonDocument("count", 1), ["$setOnInsert"] = new BsonDocument("createdAt", work.CreatedAt)
        }, new UpdateOptions { IsUpsert = true }, token);
        await works.InsertOneAsync(session, new BsonDocument
        {
            ["_id"] = work.WorkId.ToString("D"), ["providerId"] = Id(work.ProviderId), ["ownerId"] = Id(owner), ["category"] = work.Category,
            ["createdAt"] = work.CreatedAt, ["moderation"] = new BsonDocument { ["status"] = "approved", ["checkedAt"] = work.CheckedAt },
            ["publicGeo"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { work.PublicPoint.Lng, work.PublicPoint.Lat } },
            ["data"] = BsonDocument.Parse(JsonSerializer.Serialize(work, ProviderService.Json))
        }, cancellationToken: token);
        await receipts.InsertOneAsync(session, new BsonDocument
        {
            ["_id"] = Key(owner, key), ["workId"] = work.WorkId.ToString("D"), ["hash"] = hash, ["createdAt"] = work.CreatedAt
        }, cancellationToken: token);
        await profiles.UpdateOneAsync(session, providerFilter, CounterUpdate(1, 0, work.CreatedAt), cancellationToken: token);
        await EmitAsync(session, work, "work.created", token);
        return Result<Work>.Success(work);
    }, ct);
    public Task<WorkPage> ListAsync(Guid provider, Guid? after, int limit, Guid? viewer, CancellationToken ct) => QueryAsync(async () =>
    {
        var filter = new BsonDocument { ["providerId"] = Id(provider), ["moderation.status"] = "approved" };
        if (after is { } id) filter["_id"] = new BsonDocument("$lt", id.ToString("D"));
        var docs = await works.Find(filter).Sort(new BsonDocument("_id", -1)).Limit(limit + 1).ToListAsync(ct);
        var values = docs.Take(limit).Select(Read).ToArray();
        var savedIds = new HashSet<Guid>(); var recommended = new HashSet<Guid>();
        if (viewer is { } user && values.Length > 0)
        {
            var ids = new BsonArray(values.Select(w => (BsonValue)Id(w.WorkId)));
            var marks = await recommendations.Find(new BsonDocument { ["accountId"] = Id(user), ["workId"] = new BsonDocument("$in", ids) })
                .Project(new BsonDocument("workId", 1)).ToListAsync(ct);
            foreach (var mark in marks) recommended.Add(mark["workId"].AsBsonBinaryData.ToGuid());
            var saves = await saved.Find(new BsonDocument { ["accountId"] = Id(user), ["kind"] = "work", ["targetId"] = new BsonDocument("$in", ids) })
                .Project(new BsonDocument("targetId", 1)).ToListAsync(ct);
            foreach (var mark in saves) savedIds.Add(mark["targetId"].AsBsonBinaryData.ToGuid());
        }
        return new WorkPage(values, savedIds, recommended, docs.Count > limit);
    }, ct);
    public Task<Result<Work>> UpdateAsync(Guid owner, Guid id, Work work, CancellationToken ct) => TransactionAsync(async (session, token) =>
    {
        var filter = new BsonDocument { ["_id"] = id.ToString("D"), ["ownerId"] = Id(owner), ["providerId"] = Id(work.ProviderId), ["moderation.status"] = "approved" };
        var update = new BsonDocument("$set", new BsonDocument {
            ["data.title"] = work.Title,
            ["data.text"] = work.Text,
            ["data.tag"] = work.Tag == null ? BsonNull.Value : work.Tag,
            ["data.images"] = BsonArray.Create(work.Images.Select(i => BsonDocument.Parse(JsonSerializer.Serialize(i, ProviderService.Json)))),
            ["data.checkedAt"] = work.CheckedAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["moderation"] = new BsonDocument { ["status"] = "approved", ["checkedAt"] = work.CheckedAt }
        });
        var doc = await works.FindOneAndUpdateAsync(session, filter, update,
            new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, token);
        if (doc == null) return Fail<Work>(NexoCodes.WorkNotFound);
        var result = Read(doc);
        await EmitAsync(session, result, "work.updated", token);
        return Result<Work>.Success(result);
    }, ct);
    public Task<Result<bool>> DeleteAsync(Guid owner, Guid id, CancellationToken ct) => TransactionAsync(async (session, token) =>
    {
        // Moderation restricts public visibility, not the owner's right to erase their text.
        var filter = new BsonDocument { ["_id"] = id.ToString("D"), ["ownerId"] = Id(owner) };
        var doc = await works.Find(session, filter).FirstOrDefaultAsync(token);
        if (doc == null) return Fail<bool>(NexoCodes.WorkNotFound);
        var work = Read(doc);
        var managed = new List<(Guid Id,string Url)>();
        foreach (var image in work.Images)
        {
            if (!Uri.TryCreate(image.Url, UriKind.Absolute, out var uri) || !uri.AbsolutePath.StartsWith("/v1/discovery/media/", StringComparison.Ordinal) ||
                !Guid.TryParse(uri.Segments.Last(), out var upload)) return Fail<bool>(NexoCodes.ServerDependencyUnavailable);
            if (!await database.GetCollection<BsonDocument>("uploads").Find(session,new BsonDocument { ["_id"] = Id(upload), ["ownerId"] = Id(owner), ["purpose"] = "work", ["status"] = "approved" }).AnyAsync(token)) return Fail<bool>(NexoCodes.ServerDependencyUnavailable);
            managed.Add((upload,image.Url));
        }
        if (await works.FindOneAndDeleteAsync(session, filter, cancellationToken: token) == null) return Fail<bool>(NexoCodes.WorkNotFound);
        foreach(var image in managed)
            if(!await works.Find(session,new BsonDocument("data.images.url",image.Url)).AnyAsync(token))
                await database.GetCollection<BsonDocument>("uploads").UpdateOneAsync(session,new BsonDocument("_id",Id(image.Id)),
                    new BsonDocument("$set",new BsonDocument { ["status"]="deleted",["cleaned"]=false }),cancellationToken:token);
        await recommendations.DeleteManyAsync(session, new BsonDocument("workId", Id(id)), cancellationToken: token);
        await saved.DeleteManyAsync(session, new BsonDocument { ["kind"] = "work", ["targetId"] = Id(id) }, cancellationToken: token);
        await profiles.UpdateOneAsync(session, new BsonDocument { ["_id"] = Id(work.ProviderId), ["accountId"] = Id(owner) },
            CounterUpdate(-1, -work.Recs, DateTime.UtcNow), cancellationToken: token);
        await EmitAsync(session, work, "work.deleted", token);
        return Result<bool>.Success(true);
    }, ct);
    public Task<Result<WorkRecommendation>> RecommendAsync(Guid viewer, Guid id, bool desired, CancellationToken ct) => TransactionAsync(async (session, token) =>
    {
        var doc = await works.Find(session, Approved(id)).FirstOrDefaultAsync(token);
        if (doc == null) return Fail<WorkRecommendation>(NexoCodes.WorkNotFound);
        if (doc["ownerId"] == Id(viewer)) return Fail<WorkRecommendation>(NexoCodes.WorkOwnContent);
        var work = Read(doc);
        var providerFilter = new BsonDocument { ["_id"] = Id(work.ProviderId), ["published"] = true };
        if (!await profiles.Find(session, providerFilter).AnyAsync(token)) return Fail<WorkRecommendation>(NexoCodes.WorkNotFound);
        var key = new BsonDocument { ["accountId"] = Id(viewer), ["workId"] = Id(id) };
        int delta;
        if (desired)
        {
            var upsert = await recommendations.UpdateOneAsync(session, key, new BsonDocument("$setOnInsert", new BsonDocument("createdAt", DateTime.UtcNow)), new UpdateOptions { IsUpsert = true }, token);
            delta = upsert.UpsertedId != null ? 1 : 0;
        }
        else delta = (await recommendations.DeleteOneAsync(session, key, cancellationToken: token)).DeletedCount == 1 ? -1 : 0;
        if (delta != 0)
        {
            var updated = await works.FindOneAndUpdateAsync(session, Approved(id), new BsonDocument("$inc", new BsonDocument("data.recs", delta)),
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, token);
            work = Read(updated);
            if (work.Recs < 0) throw new WorkUnavailableException(); // abort, never commit an invalid counter.
            await profiles.UpdateOneAsync(session, providerFilter, CounterUpdate(0, delta, DateTime.UtcNow), cancellationToken: token);
            await EmitAsync(session, work, desired ? "work.recommended" : "work.recommendation_removed", token);
        }
        return Result<WorkRecommendation>.Success(new(id, desired, work.Recs));
    }, ct);
    private static UpdateDefinition<BsonDocument> CounterUpdate(int jobs, int recs, DateTime now)
    {
        static BsonDocument Add(string field, int amount) => new("$add", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$" + field, 0 }), amount });
        var set = new BsonDocument { ["revision"] = Add("revision", 1), ["data.updatedAt"] = now.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture) };
        if (jobs != 0) set["data.jobs"] = new BsonDocument("$max", new BsonArray { 0, Add("data.jobs", jobs) });
        if (recs != 0) set["data.recs"] = new BsonDocument("$max", new BsonArray { 0, Add("data.recs", recs) });
        return new PipelineUpdateDefinition<BsonDocument>(new[] { new BsonDocument("$set", set) });
    }
    private Task EmitAsync(IClientSessionHandle session, Work work, string type, CancellationToken ct) => events.InsertOneAsync(session, new BsonDocument
    {
        ["_id"] = Id(Guid.CreateVersion7()), ["type"] = type, ["workId"] = Id(work.WorkId), ["providerId"] = Id(work.ProviderId),
        ["category"] = work.Category, ["publicPoint"] = new BsonDocument { ["lat"] = work.PublicPoint.Lat, ["lng"] = work.PublicPoint.Lng }, ["createdAt"] = DateTime.UtcNow
    }, cancellationToken: ct);
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se puede completar esta operación sobre el trabajo.");
}

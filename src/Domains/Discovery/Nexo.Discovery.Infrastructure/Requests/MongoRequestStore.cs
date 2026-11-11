using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Common.Mongo;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Requests;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Infrastructure.Requests;

public sealed class MongoRequestStore([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : IRequestStore
{
    private readonly IMongoCollection<BsonDocument> requests = database.GetCollection<BsonDocument>("service_requests");
    private readonly IMongoCollection<BsonDocument> receipts = database.GetCollection<BsonDocument>("request_operations");
    private readonly IMongoCollection<BsonDocument> quotas = database.GetCollection<BsonDocument>("request_daily_quotas");
    private readonly IMongoCollection<BsonDocument> events = database.GetCollection<BsonDocument>("discovery_events");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private static BsonBinaryData Id(Guid id) => new(id, GuidRepresentation.Standard);
    private static string Key(Guid owner, Guid key) => owner.ToString("N") + ":" + key.ToString("N");
    private static BsonDocument Own(Guid owner, Guid id) => new() { ["_id"] = id.ToString("D"), ["clientId"] = Id(owner) };
    private static ServiceRequest Read(BsonDocument d) => JsonSerializer.Deserialize<ServiceRequest>(d["data"].AsBsonDocument.ToJson(
        new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }), ProviderService.Json)!;
    private static BsonDocument Safe => new() { ["location"] = 0, ["searchText"] = 0, ["outcome"] = 0, ["declaredProviderId"] = 0 };
    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await requests.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Geo2DSphere("location").Ascending("category").Ascending("status"), new CreateIndexOptions { Name = "ix_request_geo_category_status" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("clientId").Descending("_id"), new CreateIndexOptions { Name = "ix_request_owner_cursor" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("status").Ascending("urgencyRank").Descending("_id"), new CreateIndexOptions { Name = "ix_request_open_urgency" })
            ], ct);
            foreach (var collection in new[] { receipts, quotas, events })
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("createdAt"), new CreateIndexOptions { Name = "ix_created" }), cancellationToken: ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }
    private async Task<T> QueryAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try { await EnsureAsync(ct); return await action(); }
        catch (Exception e) when (DependencyErrors.IsUnavailable(e) || e is NotSupportedException) { throw new RequestUnavailableException(); }
    }
    private Task<Result<T>> TransactionAsync<T>(Func<IClientSessionHandle, CancellationToken, Task<Result<T>>> action, CancellationToken ct) => QueryAsync(async () =>
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var session = await database.Client.StartSessionAsync(cancellationToken: ct);
            try { return await session.WithTransactionAsync(action, new TransactionOptions(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority), ct); }
            catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey && attempt < 3) { }
            catch (MongoCommandException e) when (e.Code == 11000 && attempt < 3) { }
        }
        throw new RequestUnavailableException();
    }, ct);
    public Task<RequestReceipt?> ReceiptAsync(Guid owner, Guid key, CancellationToken ct) => QueryAsync(async () =>
    {
        var doc = await receipts.Find(new BsonDocument("_id", Key(owner, key))).FirstOrDefaultAsync(ct);
        return doc == null ? null : new RequestReceipt(doc["hash"].AsString, Guid.Parse(doc["requestId"].AsString));
    }, ct);
    public Task<ServiceRequest?> OwnAsync(Guid owner, Guid id, CancellationToken ct) => QueryAsync(async () =>
    {
        var doc = await requests.Find(Own(owner, id)).Project(Safe).FirstOrDefaultAsync(ct);
        return doc == null ? null : Read(doc);
    }, ct);
    public Task<RequestCandidate?> VisibleAsync(Guid viewer, Guid id, ProviderPoint point, double radiusKm, CancellationToken ct) => QueryAsync(async () =>
    {
        var filter = new BsonDocument
        {
            ["_id"] = id.ToString("D"),
            ["clientId"] = new BsonDocument("$ne", Id(viewer)),
            ["status"] = new BsonDocument("$in", new BsonArray { "open", "assigned" }),
            ["moderation.status"] = "approved"
        };
        var near = new BsonDocument
        {
            ["near"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { point.Lng, point.Lat } },
            ["distanceField"] = "distanceMeters",
            ["key"] = "location",
            ["spherical"] = true,
            ["maxDistance"] = radiusKm * 1000,
            ["query"] = filter
        };
        var doc = await requests.Aggregate<BsonDocument>(new BsonDocument[]
        {
            new("$geoNear", near), new("$limit", 1), new("$project", Safe)
        }, new AggregateOptions { MaxTime = TimeSpan.FromSeconds(3) }).FirstOrDefaultAsync(ct);
        return doc == null ? null : new RequestCandidate(doc["clientId"].AsBsonBinaryData.ToGuid(), Read(doc), doc["distanceMeters"].ToDouble() / 1000);
    }, ct);
    public Task<Result<ServiceRequest>> CreateAsync(Guid owner, Guid key, string hash, ServiceRequest request, ProviderPoint exact, CancellationToken ct) => TransactionAsync(async (session, token) =>
    {
        var receipt = await receipts.Find(session, new BsonDocument("_id", Key(owner, key))).FirstOrDefaultAsync(token);
        if (receipt != null)
        {
            if (receipt["hash"] != hash) return Fail<ServiceRequest>(NexoCodes.RequestIdempotencyKeyReused);
            var old = await requests.Find(session, Own(owner, Guid.Parse(receipt["requestId"].AsString))).Project(Safe).FirstOrDefaultAsync(token);
            return old == null ? Fail<ServiceRequest>(NexoCodes.RequestNotFound) : Result<ServiceRequest>.Success(Read(old));
        }
        var quotaId = owner.ToString("N") + ":" + request.CreatedAt.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var quota = await quotas.Find(session, new BsonDocument("_id", quotaId)).FirstOrDefaultAsync(token);
        if (quota?.GetValue("count", 0).ToInt32() >= 10) return Fail<ServiceRequest>(NexoCodes.RateLimitExceeded);
        await quotas.UpdateOneAsync(session, new BsonDocument("_id", quotaId), new BsonDocument
        {
            ["$inc"] = new BsonDocument("count", 1),
            ["$setOnInsert"] = new BsonDocument("createdAt", request.CreatedAt)
        }, new UpdateOptions { IsUpsert = true }, token);
        await requests.InsertOneAsync(session, new BsonDocument
        {
            ["_id"] = request.RequestId.ToString("D"),
            ["clientId"] = Id(owner),
            ["category"] = request.Category,
            ["status"] = "open",
            ["urgencyRank"] = RequestRules.Rank(request.Urgency),
            ["createdAt"] = request.CreatedAt,
            ["searchText"] = DirectoryCatalog.Normalize(request.Title + " " + request.Description),
            ["location"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { exact.Lng, exact.Lat } },
            ["moderation"] = new BsonDocument { ["status"] = "approved", ["checkedAt"] = request.CheckedAt },
            ["data"] = BsonDocument.Parse(JsonSerializer.Serialize(request, ProviderService.Json))
        }, cancellationToken: token);
        await receipts.InsertOneAsync(session, new BsonDocument { ["_id"] = Key(owner, key), ["hash"] = hash, ["requestId"] = request.RequestId.ToString("D"), ["createdAt"] = request.CreatedAt }, cancellationToken: token);
        await EmitAsync(session, request, "request.created", token);
        return Result<ServiceRequest>.Success(request);
    }, ct);
    public Task<Result<ServiceRequest>> UpdateAsync(Guid owner, Guid id, ServiceRequest request, CancellationToken ct) => TransactionAsync(async (session, token) =>
    {
        var filter = Own(owner, id);
        filter["status"] = "open";
        var update = new BsonDocument("$set", new BsonDocument
        {
            ["data.title"] = request.Title,
            ["data.description"] = request.Description,
            ["data.urgency"] = request.Urgency,
            ["data.contact"] = request.Contact,
            ["data.imageUrl"] = request.ImageUrl == null ? BsonNull.Value : request.ImageUrl,
            ["data.imageUrls"] = new BsonArray(request.Photos),
            ["data.checkedAt"] = request.CheckedAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["urgencyRank"] = RequestRules.Rank(request.Urgency),
            ["searchText"] = DirectoryCatalog.Normalize(request.Title + " " + request.Description),
            ["moderation"] = new BsonDocument { ["status"] = "approved", ["checkedAt"] = request.CheckedAt }
        });
        var doc = await requests.FindOneAndUpdateAsync(session, filter, update, new FindOneAndUpdateOptions<BsonDocument>
        { ReturnDocument = ReturnDocument.After, Projection = Safe }, token);
        if (doc == null) return Fail<ServiceRequest>(NexoCodes.RequestNotFound);
        var result = Read(doc);
        await EmitAsync(session, result, "request.updated", token);
        return Result<ServiceRequest>.Success(result);
    }, ct);
    public Task<ServiceRequest[]> MineAsync(Guid owner, string status, Guid? after, int limit, CancellationToken ct) => QueryAsync(async () =>
    {
        var filter = new BsonDocument("clientId", Id(owner));
        if (status != "all") filter["status"] = status;
        if (after is { } id) filter["_id"] = new BsonDocument("$lt", id.ToString("D"));
        var docs = await requests.Find(filter).Project(Safe).Sort(new BsonDocument("_id", -1)).Limit(limit).ToListAsync(ct);
        return docs.Select(Read).ToArray();
    }, ct);
    public Task<IReadOnlyList<RequestCandidate>> CandidatesAsync(Guid viewer, ProviderPoint point, double? radiusKm, string[]? cats, string text, bool today, CancellationToken ct) => QueryAsync<IReadOnlyList<RequestCandidate>>(async () =>
    {
        var filter = new BsonDocument { ["status"] = "open", ["moderation.status"] = "approved", ["clientId"] = new BsonDocument("$ne", Id(viewer)) };
        if (cats != null) filter["category"] = new BsonDocument("$in", new BsonArray(cats));
        if (today) filter["urgencyRank"] = 0;
        var words = DirectoryCatalog.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
        if (words.Length > 0) filter["$and"] = new BsonArray(words.Select(w => new BsonDocument("searchText", new BsonRegularExpression(Regex.Escape(w)))));
        var near = new BsonDocument
        {
            ["near"] = new BsonDocument { ["type"] = "Point", ["coordinates"] = new BsonArray { point.Lng, point.Lat } },
            ["distanceField"] = "distanceMeters",
            ["key"] = "location",
            ["spherical"] = true,
            ["query"] = filter
        };
        if (radiusKm.HasValue) near["maxDistance"] = radiusKm.Value * 1000;
        var docs = await requests.Aggregate<BsonDocument>(new BsonDocument[]
        {
            new("$geoNear", near), new("$limit", 1001), new("$project", Safe)
        }, new AggregateOptions { MaxTime = TimeSpan.FromSeconds(5) }).ToListAsync(ct);
        if (docs.Count > 1000) throw new RequestUnavailableException();
        return docs.Select(d => new RequestCandidate(d["clientId"].AsBsonBinaryData.ToGuid(), Read(d), d["distanceMeters"].ToDouble() / 1000)).ToArray();
    }, ct);
    public Task<Result<RequestClosure>> CloseAsync(Guid owner, Guid id, string outcome, Guid? declaredProvider, DateTime now, CancellationToken ct) => TransactionAsync(async (session, token) =>
    {
        var filter = Own(owner, id); filter["status"] = new BsonDocument("$ne", "closed");
        var update = new BsonDocument
        {
            ["status"] = "closed",
            ["data.status"] = "closed",
            ["data.closedAt"] = now.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["outcome"] = outcome,
            ["declaredProviderId"] = declaredProvider == null ? BsonNull.Value : Id(declaredProvider.Value)
        };
        // Return only safe fields even on a write: the exact point never leaves Mongo's projection.
        var doc = await requests.FindOneAndUpdateAsync(session, filter, new BsonDocument("$set", update), new FindOneAndUpdateOptions<BsonDocument>
        {
            ReturnDocument = ReturnDocument.After,
            Projection = Safe
        }, token);
        if (doc == null)
        {
            var previous = await requests.Find(session, Own(owner, id)).Project(Safe).FirstOrDefaultAsync(token);
            return previous == null ? Fail<RequestClosure>(NexoCodes.RequestNotFound) : Result<RequestClosure>.Success(new(Read(previous), true));
        }
        var request = Read(doc);
        await EmitAsync(session, request, "request.closed", token);
        return Result<RequestClosure>.Success(new(request, false));
    }, ct);
    private Task EmitAsync(IClientSessionHandle session, ServiceRequest request, string type, CancellationToken ct) => events.InsertOneAsync(session, new BsonDocument
    {
        ["_id"] = Id(Guid.CreateVersion7()),
        ["type"] = type,
        ["requestId"] = Id(request.RequestId),
        ["category"] = request.Category,
        ["publicPoint"] = new BsonDocument { ["lat"] = request.PublicPoint.Lat, ["lng"] = request.PublicPoint.Lng },
        ["createdAt"] = DateTime.UtcNow
    }, cancellationToken: ct);
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se puede completar esta operación sobre la solicitud.");
}

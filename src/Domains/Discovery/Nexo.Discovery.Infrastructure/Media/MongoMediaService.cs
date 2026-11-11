using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Media;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Contracts;

namespace Nexo.Discovery.Infrastructure.Media;

public sealed class MongoMediaService([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase db,
    IMediaStorage storage, IImageModeration moderation, IOptions<MediaOptions> options, IHostEnvironment environment,
    IProviderAccounts accounts, IProviderStore profiles) : IMediaService, IMediaContract
{
    private IMongoCollection<BsonDocument> Uploads => db.GetCollection<BsonDocument>("uploads");
    private static BsonBinaryData Id(Guid id) => new(id, GuidRepresentation.Standard);
    private static BsonDocument Own(Guid owner, Guid id) => new() { ["_id"] = Id(id), ["ownerId"] = Id(owner) };
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación de imagen.");
    private string BaseUrl
    {
        get
        {
            var value = options.Value.PublicBaseUrl.TrimEnd('/');
            if (value.Length == 0 && environment.IsDevelopment()) value = "http://127.0.0.1:5188";
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 ||
                (uri.Scheme != "https" && !(environment.IsDevelopment() && uri.Scheme == "http" && uri.IsLoopback))) throw new InvalidOperationException("MediaStorage PublicBaseUrl requerido");
            return value;
        }
    }
    public async Task<Result<UploadTicket>> CreateAsync(Guid owner, UploadDraft draft, CancellationToken ct)
    {
        if (MediaRules.Validate(draft) is { } error) return Fail<UploadTicket>(error);
        if ((await accounts.ReadAsync(owner, ct))?.Active != true) return Fail<UploadTicket>("account.pending_deletion");
        if (draft.Purpose is "work" or "portfolio" && (await profiles.OwnAsync(owner, ct))?.Published != true) return Fail<UploadTicket>("provider.profile_required");
        _ = BaseUrl;
        await storage.EnsureUploadInfrastructureAsync(ct);
        await Uploads.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("status").Ascending("nextAttempt")), cancellationToken: ct);
        await Uploads.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("ownerId").Ascending("createdAt")), cancellationToken: ct);
        var id = Guid.CreateVersion7(); var ticket = storage.Ticket(id); var now = DateTime.UtcNow;
        using var session = await db.Client.StartSessionAsync(cancellationToken: ct);
        return await session.WithTransactionAsync(async (s, token) =>
        {
            var quota = db.GetCollection<BsonDocument>("media_quotas"); var key = owner.ToString("N") + ":" + now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
            var count = await quota.Find(s, new BsonDocument("_id", key)).FirstOrDefaultAsync(token);
            if (count?.GetValue("count", 0).ToInt32() >= 30) return Fail<UploadTicket>("rate.limit_exceeded");
            await quota.UpdateOneAsync(s, new BsonDocument("_id", key), new BsonDocument("$inc", new BsonDocument("count", 1)), new UpdateOptions { IsUpsert = true }, token);
            var doc = Own(owner, id); doc["purpose"] = draft.Purpose; doc["contentType"] = draft.ContentType; doc["size"] = draft.Size;
            doc["status"] = "pending"; doc["createdAt"] = now; doc["expiresAt"] = now.AddMinutes(15); doc["nextAttempt"] = now; doc["attempts"] = 0;
            await Uploads.InsertOneAsync(s, doc, cancellationToken: token);
            return Result<UploadTicket>.Success(new(id, ticket, new() { ["x-ms-blob-type"] = "BlockBlob", ["Content-Type"] = draft.ContentType }));
        }, new TransactionOptions(ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority), ct);
    }
    private UploadState State(BsonDocument d)
    {
        var id = d["_id"].AsBsonBinaryData.ToGuid(); var status = d["status"].AsString;
        return new(id, status, status == "approved" ? d["url"].AsString : null, status == "approved" ? d["thumbUrl"].AsString : null,
            d.TryGetValue("w", out var w) ? w.AsInt32 : null, d.TryGetValue("h", out var h) ? h.AsInt32 : null, d.TryGetValue("code", out var c) ? c.AsString : null);
    }
    public async Task<Result<UploadState>> StateAsync(Guid owner, Guid id, CancellationToken ct)
    {
        var doc = await Uploads.Find(Own(owner, id)).FirstOrDefaultAsync(ct);
        if (doc == null || doc["status"] == "deleted") return Fail<UploadState>("upload.not_found");
        if (doc["status"] == "pending" && doc["expiresAt"].ToUniversalTime() < DateTime.UtcNow) return Fail<UploadState>("upload.expired");
        if (doc["status"] == "rejected") return Fail<UploadState>(doc.GetValue("code", "upload.rejected_by_moderation").AsString);
        return Result<UploadState>.Success(State(doc));
    }
    public async Task<Result<UploadState>> CompleteAsync(Guid owner, Guid id, CancellationToken ct)
    {
        var result = await StateAsync(owner, id, ct); if (result.IsFailure) return result;
        if (result.Value!.Status != "pending") return result;
        if (!await storage.ExistsAsync(id, ct)) return Fail<UploadState>("upload.blob_missing");
        var filter = Own(owner, id); filter["status"] = "pending"; filter["expiresAt"] = new BsonDocument("$gt", DateTime.UtcNow);
        await Uploads.UpdateOneAsync(filter, new BsonDocument("$set", new BsonDocument { ["status"] = "processing", ["nextAttempt"] = DateTime.UtcNow }), cancellationToken: ct);
        await storage.EnqueueAsync(id, ct); // durable processing row is recovered by worker if enqueue fails.
        return await StateAsync(owner, id, ct);
    }
    public async Task<string?> AvatarAsync(Guid owner, Guid upload, CancellationToken ct)
    {
        var filter = Own(owner, upload); filter["status"] = "approved"; filter["purpose"] = "avatar";
        var doc = await Uploads.Find(filter).FirstOrDefaultAsync(ct); return doc?.GetValue("url").AsString;
    }
    public async Task<MediaFile?> FileAsync(Guid id, bool thumb, CancellationToken ct)
    {
        var doc = await Uploads.Find(new BsonDocument { ["_id"] = Id(id), ["status"] = "approved" }).FirstOrDefaultAsync(ct);
        if (doc == null || (await accounts.ReadAsync(doc["ownerId"].AsBsonBinaryData.ToGuid(), ct))?.Active != true) return null;
        var bytes = await storage.ReadAsync(doc["path"].AsString + (thumb ? ".thumb" : ""), ct);
        // Retraction concurrent with download must not be served.
        if (!await Uploads.Find(new BsonDocument { ["_id"] = Id(id), ["status"] = "approved" }).AnyAsync(ct)) return null;
        return new(bytes, "image/webp");
    }
    public async Task<Result<bool>> DeleteAsync(Guid owner, Guid id, CancellationToken ct)
    {
        var doc = await Uploads.FindOneAndUpdateAsync(Own(owner, id), new BsonDocument("$set", new BsonDocument { ["status"] = "deleted", ["cleaned"] = false }), cancellationToken: ct);
        if (doc == null) return Fail<bool>("upload.not_found");
        await storage.RemoveAsync(id, doc.GetValue("path", BsonNull.Value).IsString ? doc["path"].AsString : null, ct);
        await Uploads.UpdateOneAsync(Own(owner, id), new BsonDocument("$set", new BsonDocument("cleaned", true)), cancellationToken: ct);
        return Result<bool>.Success(true);
    }
    public async Task<string?> CoverAsync(Guid owner, CancellationToken ct)
    {
        var doc = await db.GetCollection<BsonDocument>("account_covers").Find(new BsonDocument("_id", Id(owner))).FirstOrDefaultAsync(ct);
        if (doc == null) return null; var state = await StateAsync(owner, doc["uploadId"].AsBsonBinaryData.ToGuid(), ct); return state.IsSuccess ? state.Value!.Url : null;
    }
    public async Task<Result<string>> SetCoverAsync(Guid owner, Guid id, CancellationToken ct)
    {
        var filter = Own(owner, id); filter["status"] = "approved"; filter["purpose"] = "cover";
        var doc = await Uploads.Find(filter).FirstOrDefaultAsync(ct); if (doc == null) return Fail<string>("upload.not_found");
        var previous = await db.GetCollection<BsonDocument>("account_covers").FindOneAndUpdateAsync(new BsonDocument("_id", Id(owner)), new BsonDocument("$set", new BsonDocument("uploadId", Id(id))), new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true }, ct);
        _ = previous; // Audit reconciles replaced cover without deleting a shared profile reference.
        return Result<string>.Success(doc["url"].AsString);
    }
    public async Task ProcessAsync(Guid id, CancellationToken ct)
    {
        var now = DateTime.UtcNow; var lease = Guid.NewGuid().ToString("N");
        var doc = await Uploads.FindOneAndUpdateAsync(new BsonDocument { ["_id"] = Id(id), ["status"] = "processing", ["nextAttempt"] = new BsonDocument("$lte", now) },
            new BsonDocument { ["$set"] = new BsonDocument { ["lease"] = lease, ["nextAttempt"] = now.AddMinutes(2) }, ["$inc"] = new BsonDocument("attempts", 1) }, new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, ct);
        if (doc == null) return;
        var filter = new BsonDocument { ["_id"] = Id(id), ["status"] = "processing", ["lease"] = lease };
        var path = id.ToString("N") + "/image.webp";
        try
        {
            var owner = doc["ownerId"].AsBsonBinaryData.ToGuid();
            if ((await accounts.ReadAsync(owner, ct))?.Active != true) throw new InvalidDataException("account.pending_deletion");
            var url = BaseUrl + "/v1/discovery/media/" + id.ToString("D");
            if (doc.GetValue("verified", false) != true)
            {
                var original = await storage.ReadIncomingAsync(id, doc["size"].AsInt32, ct);
                var encoded = ImageCodec.Encode(original, doc["contentType"].AsString);
                if (!await moderation.ApproveAsync(encoded.Moderation, ct)) throw new InvalidDataException("upload.rejected_by_moderation");
                await storage.WriteAsync(path, encoded.Full, ct); await storage.WriteAsync(path + ".thumb", encoded.Thumb, ct);
                // Durable stage before original deletion: a crash resumes without needing the erased source.
                var stage = new BsonDocument { ["verified"] = true, ["path"] = path, ["url"] = url, ["thumbUrl"] = url + "/thumb", ["w"] = encoded.Width, ["h"] = encoded.Height, ["bytes"] = encoded.Full.Length };
                var staged = await Uploads.UpdateOneAsync(filter, new BsonDocument("$set", stage), cancellationToken: ct);
                if (staged.ModifiedCount == 0) { await storage.RemoveAsync(id, path, ct); return; }
            }
            await storage.RemoveAsync(id, null, ct);
            var changed = await Uploads.UpdateOneAsync(filter, new BsonDocument("$set", new BsonDocument { ["status"] = "approved", ["approvedAt"] = DateTime.UtcNow, ["nextAudit"] = DateTime.UtcNow.AddHours(1) }), cancellationToken: ct);
            if (changed.ModifiedCount == 0) await storage.RemoveAsync(id, path, ct);
        }
        catch (InvalidDataException error)
        {
            await storage.RemoveAsync(id, path, ct);
            await Uploads.UpdateOneAsync(filter, new BsonDocument("$set", new BsonDocument { ["status"] = "rejected", ["code"] = error.Message }), cancellationToken: ct);
        }
        catch (Exception error) when (error is Azure.RequestFailedException or HttpRequestException or TimeoutException or System.Text.Json.JsonException || error is OperationCanceledException && !ct.IsCancellationRequested)
        {
            await Uploads.UpdateOneAsync(filter, new BsonDocument("$set", new BsonDocument { ["nextAttempt"] = DateTime.UtcNow.AddSeconds(15), ["code"] = "server.dependency_unavailable" }), cancellationToken: ct);
        }
    }
    private async Task<bool> ReferencedAsync(BsonDocument upload, ProviderAccountState account, CancellationToken ct)
    {
        var owner = upload["ownerId"]; var url = upload["url"].AsString;
        if (account.PhotoUrl == url) return true;
        if (await db.GetCollection<BsonDocument>("provider_profiles").Find(new BsonDocument { ["accountId"] = owner, ["$or"] = new BsonArray { new BsonDocument("data.photoUrl", url), new BsonDocument("data.coverUrl", url), new BsonDocument("data.portfolio.url", url) } }).AnyAsync(ct)) return true;
        if (await db.GetCollection<BsonDocument>("works").Find(new BsonDocument("data.images.url", url)).AnyAsync(ct)) return true;
        if (await db.GetCollection<BsonDocument>("service_requests").Find(new BsonDocument("$or", new BsonArray { new BsonDocument("data.imageUrl", url), new BsonDocument("data.imageUrls", url) })).AnyAsync(ct)) return true;
        return await db.GetCollection<BsonDocument>("account_covers").Find(new BsonDocument { ["_id"] = owner, ["uploadId"] = upload["_id"] }).AnyAsync(ct);
    }
    public async Task AuditAsync(CancellationToken ct)
    {
        var docs = await Uploads.Find(new BsonDocument { ["status"] = "approved", ["nextAudit"] = new BsonDocument("$lte", DateTime.UtcNow) }).Limit(10).ToListAsync(ct);
        foreach (var doc in docs)
        {
            var owner = doc["ownerId"].AsBsonBinaryData.ToGuid(); var account = await accounts.ReadAsync(owner, ct);
            // Pending deletion is reversible: never purge solely because Active is false.
            if (account?.Deleted == true || account is { Active: true } && !await ReferencedAsync(doc, account, ct))
                await DeleteAsync(owner, doc["_id"].AsBsonBinaryData.ToGuid(), ct);
            else await Uploads.UpdateOneAsync(new BsonDocument("_id", doc["_id"]), new BsonDocument("$set", new BsonDocument("nextAudit", DateTime.UtcNow.AddHours(1))), cancellationToken: ct);
        }
    }
    public async Task RecoverAsync(CancellationToken ct)
    {
        var docs = await Uploads.Find(new BsonDocument("$or", new BsonArray {
            new BsonDocument { ["status"]="processing",["nextAttempt"]=new BsonDocument("$lte",DateTime.UtcNow) },
            new BsonDocument { ["status"]="pending",["expiresAt"]=new BsonDocument("$lt",DateTime.UtcNow) },
            new BsonDocument { ["status"]="deleted",["cleaned"]=new BsonDocument("$ne",true) }
        })).Limit(20).ToListAsync(ct);
        foreach (var doc in docs)
        {
            var id = doc["_id"].AsBsonBinaryData.ToGuid();
            if (doc["status"] == "processing") await storage.EnqueueAsync(id, ct);
            else await DeleteAsync(doc["ownerId"].AsBsonBinaryData.ToGuid(), id, ct);
        }
    }
}

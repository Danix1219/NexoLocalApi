using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Saved;

namespace Nexo.Discovery.Infrastructure.Saved;

public sealed class MongoSavedStore([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : ISavedStore
{
    private readonly IMongoCollection<BsonDocument> saved = database.GetCollection<BsonDocument>("saved");
    private readonly IMongoCollection<BsonDocument> providers = database.GetCollection<BsonDocument>("provider_profiles");
    private readonly IMongoCollection<BsonDocument> works = database.GetCollection<BsonDocument>("works");
    private readonly IMongoCollection<BsonDocument> requests = database.GetCollection<BsonDocument>("service_requests");
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private static BsonBinaryData Id(Guid id) => new(id, GuidRepresentation.Standard);

    private async Task EnsureAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (initialized) return;
            await saved.Indexes.CreateManyAsync([
                new(Builders<BsonDocument>.IndexKeys.Ascending("accountId").Ascending("kind").Ascending("targetId"),
                    new CreateIndexOptions { Unique = true, Name = "ux_saved_account_kind_target" }),
                new(Builders<BsonDocument>.IndexKeys.Ascending("accountId").Descending("createdAt").Descending("targetId"),
                    new CreateIndexOptions { Name = "ix_saved_account_created_target" })
            ], ct);
            initialized = true;
        }
        finally { gate.Release(); }
    }

    public async Task<SavedMutation?> SetAsync(Guid accountId, string kind, Guid targetId, bool desired, DateTime now, CancellationToken ct)
    {
        try
        {
            await EnsureAsync(ct);
            var identity = new BsonDocument { ["accountId"] = Id(accountId), ["kind"] = kind, ["targetId"] = Id(targetId) };
            if (desired)
            {
                var target = await FindTargetAsync(accountId, kind, targetId, ct);
                if (target is null) return null;
                await saved.UpdateOneAsync(identity,
                    new BsonDocument("$setOnInsert", new BsonDocument("createdAt", now)),
                    new UpdateOptions { IsUpsert = true }, ct);
            }
            else
            {
                await saved.DeleteOneAsync(identity, ct);
            }
            var counts = await CountsAsync(accountId, ct);
            return new SavedMutation(kind, targetId, desired, counts);
        }
        catch (Exception e) when (DependencyErrors.IsUnavailable(e) || e is NotSupportedException)
        {
            throw new SavedUnavailableException(e);
        }
    }

    public async Task<SavedPage> ListAsync(Guid accountId, string? kind, string? cursor, int limit, CancellationToken ct)
    {
        try
        {
            await EnsureAsync(ct);
            var filter = new BsonDocument("accountId", Id(accountId));
            if (kind is not null) filter["kind"] = kind;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                if (!TryDecode(cursor, out var at, out var target)) throw new ArgumentException("Invalid saved cursor.");
                filter["$or"] = new BsonArray
                {
                    new BsonDocument("createdAt", new BsonDocument("$lt", at)),
                    new BsonDocument { ["createdAt"] = at, ["targetId"] = new BsonDocument("$lt", Id(target)) }
                };
            }
            var page = await saved.Find(filter).Sort(new BsonDocument { ["createdAt"] = -1, ["targetId"] = -1 })
                .Limit(limit + 1).ToListAsync(ct);
            var hasMore = page.Count > limit;
            if (hasMore) page.RemoveAt(page.Count - 1);
            var entries = page.Select(d => new SavedKey(d["kind"].AsString, d["targetId"].AsBsonBinaryData.ToGuid(), d["createdAt"].ToUniversalTime())).ToArray();
            var payloads = await ReadPayloadsAsync(entries, ct);
            var items = entries.Select(entry =>
            {
                payloads.TryGetValue((entry.Kind, entry.TargetId), out var payload);
                var available = payload.ValueKind != JsonValueKind.Undefined;
                return new SavedItem(entry.Kind, entry.TargetId, entry.SavedAt, available,
                    available ? null : "unpublished",
                    entry.Kind == "provider" && available ? payload : null,
                    entry.Kind == "work" && available ? payload : null,
                    entry.Kind == "request" && available ? payload : null);
            }).ToArray();
            var next = hasMore && entries.Length > 0 ? Encode(entries[^1].SavedAt, entries[^1].TargetId) : null;
            return new SavedPage(await CountsAsync(accountId, ct), items, next, hasMore);
        }
        catch (ArgumentException) { throw; }
        catch (Exception e) when (DependencyErrors.IsUnavailable(e) || e is NotSupportedException)
        {
            throw new SavedUnavailableException(e);
        }
    }

    private async Task<BsonDocument?> FindTargetAsync(Guid accountId, string kind, Guid targetId, CancellationToken ct)
    {
        return kind switch
        {
            "provider" => await providers.Find(new BsonDocument { ["_id"] = Id(targetId), ["published"] = true })
                .Project(new BsonDocument { ["accountId"] = 1 }).FirstOrDefaultAsync(ct) is { } provider &&
                provider.GetValue("accountId", BsonNull.Value).IsBsonBinaryData && provider["accountId"].AsBsonBinaryData.ToGuid() != accountId ? provider : null,
            "work" => await works.Find(new BsonDocument { ["_id"] = targetId.ToString("D"), ["moderation.status"] = "approved" })
                .Project(new BsonDocument { ["ownerId"] = 1 }).FirstOrDefaultAsync(ct) is { } work &&
                work.GetValue("ownerId", BsonNull.Value).IsBsonBinaryData && work["ownerId"].AsBsonBinaryData.ToGuid() != accountId ? work : null,
            "request" => await requests.Find(new BsonDocument { ["_id"] = targetId.ToString("D"), ["status"] = new BsonDocument("$in", new BsonArray { "open", "assigned" }), ["moderation.status"] = "approved" })
                .Project(new BsonDocument { ["clientId"] = 1 }).FirstOrDefaultAsync(ct) is { } request &&
                request.GetValue("clientId", BsonNull.Value).IsBsonBinaryData && request["clientId"].AsBsonBinaryData.ToGuid() != accountId ? request : null,
            _ => null
        };
    }

    private async Task<Dictionary<(string Kind, Guid Id), JsonElement>> ReadPayloadsAsync(SavedKey[] entries, CancellationToken ct)
    {
        var result = new Dictionary<(string, Guid), JsonElement>();
        foreach (var kind in new[] { "provider", "work", "request" })
        {
            var ids = entries.Where(e => e.Kind == kind).Select(e => e.TargetId).Distinct().ToArray();
            if (ids.Length == 0) continue;
            var filter = kind switch
            {
                "provider" => new BsonDocument { ["_id"] = new BsonDocument("$in", new BsonArray(ids.Select(Id))), ["published"] = true },
                "work" => new BsonDocument { ["_id"] = new BsonDocument("$in", new BsonArray(ids.Select(id => (BsonValue)id.ToString("D")))), ["moderation.status"] = "approved" },
                _ => new BsonDocument { ["_id"] = new BsonDocument("$in", new BsonArray(ids.Select(id => (BsonValue)id.ToString("D")))), ["status"] = new BsonDocument("$in", new BsonArray { "open", "assigned" }), ["moderation.status"] = "approved" }
            };
            var collection = kind switch { "provider" => providers, "work" => works, _ => requests };
            var docs = await collection.Find(filter).Project(new BsonDocument { ["_id"] = 1, ["data"] = 1 }).ToListAsync(ct);
            foreach (var doc in docs)
            {
                var id = kind == "provider" ? doc["_id"].AsBsonBinaryData.ToGuid() : Guid.Parse(doc["_id"].AsString);
                var data = doc.GetValue("data", BsonNull.Value);
                if (!data.IsBsonDocument) continue;
                var json = JsonDocument.Parse(data.AsBsonDocument.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson }));
                var safe = json.RootElement.Clone();
                if (safe.ValueKind == JsonValueKind.Object && safe.TryGetProperty("location", out _))
                {
                    using var buffer = new MemoryStream();
                    using (var writer = new Utf8JsonWriter(buffer))
                    {
                        writer.WriteStartObject();
                        foreach (var property in safe.EnumerateObject()) if (property.Name != "location") property.WriteTo(writer);
                        writer.WriteEndObject();
                    }
                    safe = JsonDocument.Parse(buffer.ToArray()).RootElement.Clone();
                }
                result[(kind, id)] = safe;
            }
        }
        return result;
    }

    private async Task<SavedCounts> CountsAsync(Guid accountId, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("accountId", Id(accountId))),
            new BsonDocument("$group", new BsonDocument { ["_id"] = "$kind", ["count"] = new BsonDocument("$sum", 1) })
        };
        var counts = await saved.Aggregate<BsonDocument>(pipeline).ToListAsync(ct);
        var values = counts.ToDictionary(x => x["_id"].AsString, x => x["count"].ToInt32());
        return new SavedCounts(values.GetValueOrDefault("provider"), values.GetValueOrDefault("work"), values.GetValueOrDefault("request"));
    }

    private static string Encode(DateTime time, Guid id) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{time.Ticks}:{id:N}"))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static bool TryDecode(string cursor, out DateTime time, out Guid id)
    {
        time = default; id = default;
        try
        {
            var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor.Replace('-', '+').Replace('_', '/') + new string('=', (4 - cursor.Length % 4) % 4)));
            var parts = raw.Split(':');
            return parts.Length == 2 && long.TryParse(parts[0], out var ticks) && Guid.TryParseExact(parts[1], "N", out id) &&
                (time = new DateTime(ticks, DateTimeKind.Utc)) != default;
        }
        catch (Exception e) when (e is FormatException or ArgumentOutOfRangeException) { return false; }
    }
    private sealed record SavedKey(string Kind, Guid TargetId, DateTime SavedAt);
}

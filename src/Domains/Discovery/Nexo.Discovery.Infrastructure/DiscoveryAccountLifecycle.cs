using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Accounts;
using Nexo.Common.Mongo;
using Nexo.Discovery.Infrastructure.Media;

namespace Nexo.Discovery.Infrastructure;

/// <summary>
/// Parte de la baja de cuenta en Discovery (nv-12). Ocultar usa los mismos campos que ya filtran las
/// lecturas (`published` y `moderation.status`), así que ninguna consulta necesita saber de bajas.
/// </summary>
public sealed class DiscoveryAccountLifecycle(
    [FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database, IMediaStorage media)
    : IAccountDataLifecycle, IAccountSubjectResolver
{
    private const string HiddenWork = "suspended";
    private const string Flag = "suspendedByDeletion";

    public string Domain => "discovery";

    private IMongoCollection<BsonDocument> Of(string name) => database.GetCollection<BsonDocument>(name);
    private static BsonBinaryData Id(Guid value) => new(value, GuidRepresentation.Standard);
    private static BsonDocument In(IEnumerable<BsonValue> values) => new("$in", new BsonArray(values));

    public async Task<Guid?> ProviderIdAsync(Guid accountId, CancellationToken ct)
    {
        var profile = await Of("provider_profiles").Find(new BsonDocument("accountId", Id(accountId)))
            .Project(new BsonDocument("_id", 1)).FirstOrDefaultAsync(ct);
        return profile?["_id"].AsBsonBinaryData.ToGuid();
    }

    public async Task SuspendAsync(AccountSubject subject, DateTime now, CancellationToken ct)
    {
        var owner = Id(subject.AccountId);
        await Of("provider_profiles").UpdateManyAsync(
            new BsonDocument { ["accountId"] = owner, ["published"] = true },
            new BsonDocument("$set", new BsonDocument { ["published"] = false, [Flag] = true }), cancellationToken: ct);
        await Of("works").UpdateManyAsync(
            new BsonDocument { ["ownerId"] = owner, ["moderation.status"] = "approved" },
            new BsonDocument("$set", new BsonDocument("moderation.status", HiddenWork)), cancellationToken: ct);
        // Las solicitudes abiertas se cierran y no se reabren al cancelar la baja.
        var closedAt = now.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        await Of("service_requests").UpdateManyAsync(
            new BsonDocument { ["clientId"] = owner, ["status"] = "open" },
            new BsonDocument("$set", new BsonDocument
            {
                ["status"] = "closed",
                ["data.status"] = "closed",
                ["data.closedAt"] = closedAt,
                ["outcome"] = "account_deletion"
            }), cancellationToken: ct);
    }

    public async Task RestoreAsync(AccountSubject subject, DateTime now, CancellationToken ct)
    {
        var owner = Id(subject.AccountId);
        await Of("provider_profiles").UpdateManyAsync(
            new BsonDocument { ["accountId"] = owner, [Flag] = true },
            new BsonDocument { ["$set"] = new BsonDocument("published", true), ["$unset"] = new BsonDocument(Flag, "") },
            cancellationToken: ct);
        await Of("works").UpdateManyAsync(
            new BsonDocument { ["ownerId"] = owner, ["moderation.status"] = HiddenWork },
            new BsonDocument("$set", new BsonDocument("moderation.status", "approved")), cancellationToken: ct);
    }

    public async Task EraseAsync(AccountSubject subject, DateTime now, CancellationToken ct)
    {
        var owner = Id(subject.AccountId);

        // 1. Archivos primero: si el almacenamiento falla, los registros siguen para reintentar.
        var uploads = await Of("uploads").Find(new BsonDocument("ownerId", owner)).ToListAsync(ct);
        foreach (var upload in uploads)
        {
            var path = upload.TryGetValue("path", out var value) && value.IsString ? value.AsString : null;
            await media.RemoveAsync(upload["_id"].AsBsonBinaryData.ToGuid(), path, ct);
            await Of("uploads").DeleteOneAsync(new BsonDocument("_id", upload["_id"]), ct);
        }

        // 2. Publicaciones y todo lo que apunta a ellas.
        var works = await Of("works").Find(new BsonDocument("ownerId", owner)).Project(new BsonDocument("_id", 1)).ToListAsync(ct);
        var requests = await Of("service_requests").Find(new BsonDocument("clientId", owner)).Project(new BsonDocument("_id", 1)).ToListAsync(ct);
        var targets = works.Concat(requests).Select(d => (BsonValue)Id(Guid.Parse(d["_id"].AsString))).ToList();
        if (subject.ProviderId is { } providerTarget) targets.Add(Id(providerTarget));
        if (targets.Count > 0) await Of("saved").DeleteManyAsync(new BsonDocument("targetId", In(targets)), ct);
        if (works.Count > 0)
        {
            await Of("recommendations").DeleteManyAsync(
                new BsonDocument("workId", In(works.Select(d => (BsonValue)Id(Guid.Parse(d["_id"].AsString))))), ct);
        }

        await Of("works").DeleteManyAsync(new BsonDocument("ownerId", owner), ct);
        await Of("service_requests").DeleteManyAsync(new BsonDocument("clientId", owner), ct);

        // 3. Datos privados de la cuenta.
        await Of("saved").DeleteManyAsync(new BsonDocument("accountId", owner), ct);
        await Of("recommendations").DeleteManyAsync(new BsonDocument("accountId", owner), ct);
        await Of("user_profiles").DeleteOneAsync(new BsonDocument("_id", owner), ct);
        await Of("account_covers").DeleteOneAsync(new BsonDocument("_id", owner), ct);
        var receipts = new BsonDocument("_id", new BsonRegularExpression("^" + subject.AccountId.ToString("N") + ":"));
        await Of("request_operations").DeleteManyAsync(receipts, ct);
        await Of("work_operations").DeleteManyAsync(receipts, ct);

        // 4. El perfil queda como cascarón sin datos: los pedidos y opiniones conservados lo referencian.
        if (subject.ProviderId is { } providerId)
            await Of("discovery_events").DeleteManyAsync(new BsonDocument("providerId", Id(providerId)), ct);
        var blank = new BsonDocument
        {
            ["data.name"] = "Usuario eliminado",
            ["data.bio"] = "",
            ["data.owner"] = BsonNull.Value,
            ["data.activity"] = BsonNull.Value,
            ["data.photoUrl"] = BsonNull.Value,
            ["data.coverUrl"] = BsonNull.Value,
            ["data.web"] = BsonNull.Value,
            ["data.warranty"] = BsonNull.Value,
            ["data.location"] = BsonNull.Value,
            ["data.publicZone"] = BsonNull.Value,
            ["data.schedule"] = BsonNull.Value,
            ["data.prices"] = new BsonArray(),
            ["data.portfolio"] = new BsonArray(),
            ["data.trades"] = new BsonArray(),
            ["data.specialties"] = new BsonArray(),
            ["published"] = false
        };
        var remove = new BsonDocument
        {
            ["locationGeo"] = "",
            ["events"] = "",
            ["creationKey"] = "",
            ["creationHash"] = "",
            ["creationProfile"] = "",
            ["views"] = "",
            [Flag] = ""
        };
        await Of("provider_profiles").UpdateManyAsync(new BsonDocument("accountId", owner),
            new BsonDocument { ["$set"] = blank, ["$unset"] = remove }, cancellationToken: ct);
    }
}

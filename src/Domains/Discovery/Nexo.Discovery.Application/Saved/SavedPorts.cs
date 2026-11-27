using Nexo.Discovery.Application.Requests;
using Nexo.Common.Results;
using System.Text.Json;

namespace Nexo.Discovery.Application.Saved;

public sealed record SavedCounts(int Provider, int Work, int Request);
public sealed record SavedItem(string Kind, Guid TargetId, DateTime SavedAt, bool Available,
    string? UnavailableReason, JsonElement? Provider, JsonElement? Work, JsonElement? Request);
public sealed record SavedPage(SavedCounts Counts, SavedItem[] Items, string? NextCursor, bool HasMore);
public sealed record SavedMutation(string Kind, Guid TargetId, bool Saved, SavedCounts SavedCounts, bool OwnContent = false);

public interface ISavedStore
{
    Task<IReadOnlyDictionary<Guid, Guid>> RequestClientIdsAsync(Guid[] requestIds, CancellationToken ct);
    Task<SavedMutation?> SetAsync(Guid accountId, string kind, Guid targetId, bool saved, DateTime now, CancellationToken ct);
    Task<SavedPage> ListAsync(Guid accountId, string? kind, string? cursor, int limit, CancellationToken ct);
    /// <summary>Owner account of a public provider/work/request; null when missing or not public.</summary>
    Task<Guid?> TargetOwnerAsync(string kind, Guid targetId, CancellationToken ct);
}

public sealed class SavedUnavailableException(Exception? inner = null) : Exception("Saved items are temporarily unavailable.", inner);

public sealed class SavedService(ISavedStore store, IRequestClients clients, TimeProvider clock)
{
    private static readonly HashSet<string> Kinds = ["provider", "work", "request"];

    public async Task<Result<SavedMutation>> SetAsync(Guid accountId, string? kind, Guid targetId, bool saved, CancellationToken ct)
    {
        if (accountId == Guid.Empty || targetId == Guid.Empty || kind is null || !Kinds.Contains(kind))
            return Result<SavedMutation>.Failure("validation_failed", "Revisa el tipo y el identificador del elemento.");
        var result = await store.SetAsync(accountId, kind, targetId, saved, clock.GetUtcNow().UtcDateTime, ct);
        if (result is null) return Result<SavedMutation>.Failure("resource.not_found", "El elemento no está disponible.");
        if (result.OwnContent) return Result<SavedMutation>.Failure(Nexo.Common.Errors.NexoCodes.SavedOwnContent, "No puedes guardar tu propio contenido.");
        return Result<SavedMutation>.Success(result);
    }

    public async Task<Result<SavedPage>> ListAsync(Guid accountId, string? kind, string? cursor, int limit, CancellationToken ct)
    {
        if (accountId == Guid.Empty || (kind is not null && kind != "all" && !Kinds.Contains(kind)))
            return Result<SavedPage>.Failure("validation_failed", "Revisa el filtro de elementos guardados.");
        if (limit is < 1 or > 50) return Result<SavedPage>.Failure("request.limit_exceeded", "El límite debe estar entre 1 y 50.");
        if (cursor is { Length: > 64 }) return Result<SavedPage>.Failure("request.cursor_invalid", "Actualiza la lista para continuar.");
        var page = await store.ListAsync(accountId, kind == "all" ? null : kind, cursor, limit, ct);
        var requestIds = page.Items.Where(i => i.Kind == "request" && i.Available).Select(i => i.TargetId).ToArray();
        if (requestIds.Length == 0) return Result<SavedPage>.Success(page);
        var owners = await store.RequestClientIdsAsync(requestIds, ct);
        var authors = await clients.ReadAsync(owners.Values.Distinct().ToArray(), ct);
        var items = page.Items.Select(item =>
        {
            if (item.Kind != "request" || !item.Available || item.Request is null) return item;
            if (!owners.TryGetValue(item.TargetId, out var owner) || !authors.TryGetValue(owner, out var author) || !author.Active)
                return item with { Available = false, UnavailableReason = "unpublished", Request = null };
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.Request.Value.GetRawText())!;
            payload["client"] = JsonSerializer.SerializeToElement(new { displayName = author.DisplayName, photoUrl = author.PhotoUrl });
            payload["saved"] = JsonSerializer.SerializeToElement(true);
            return item with { Request = JsonSerializer.SerializeToElement(payload) };
        }).ToArray();
        return Result<SavedPage>.Success(page with { Items = items });
    }
}

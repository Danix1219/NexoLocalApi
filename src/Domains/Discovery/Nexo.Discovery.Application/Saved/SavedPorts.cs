using Nexo.Common.Results;
using System.Text.Json;

namespace Nexo.Discovery.Application.Saved;

public sealed record SavedCounts(int Provider, int Work, int Request);
public sealed record SavedItem(string Kind, Guid TargetId, DateTime SavedAt, bool Available,
    string? UnavailableReason, JsonElement? Provider, JsonElement? Work, JsonElement? Request);
public sealed record SavedPage(SavedCounts Counts, SavedItem[] Items, string? NextCursor, bool HasMore);
public sealed record SavedMutation(string Kind, Guid TargetId, bool Saved, SavedCounts SavedCounts);

public interface ISavedStore
{
    Task<SavedMutation?> SetAsync(Guid accountId, string kind, Guid targetId, bool saved, DateTime now, CancellationToken ct);
    Task<SavedPage> ListAsync(Guid accountId, string? kind, string? cursor, int limit, CancellationToken ct);
}

public sealed class SavedUnavailableException(Exception? inner = null) : Exception("Saved items are temporarily unavailable.", inner);

public sealed class SavedService(ISavedStore store, TimeProvider clock)
{
    private static readonly HashSet<string> Kinds = ["provider", "work", "request"];

    public async Task<Result<SavedMutation>> SetAsync(Guid accountId, string? kind, Guid targetId, bool saved, CancellationToken ct)
    {
        if (accountId == Guid.Empty || targetId == Guid.Empty || kind is null || !Kinds.Contains(kind))
            return Result<SavedMutation>.Failure("validation_failed", "Revisa el tipo y el identificador del elemento.");
        var result = await store.SetAsync(accountId, kind, targetId, saved, clock.GetUtcNow().UtcDateTime, ct);
        if (result is null) return Result<SavedMutation>.Failure("resource.not_found", "El elemento no está disponible.");
        return Result<SavedMutation>.Success(result);
    }

    public async Task<Result<SavedPage>> ListAsync(Guid accountId, string? kind, string? cursor, int limit, CancellationToken ct)
    {
        if (accountId == Guid.Empty || (kind is not null && kind != "all" && !Kinds.Contains(kind)))
            return Result<SavedPage>.Failure("validation_failed", "Revisa el filtro de elementos guardados.");
        if (limit is < 1 or > 50) return Result<SavedPage>.Failure("request.limit_exceeded", "El límite debe estar entre 1 y 50.");
        if (cursor is { Length: > 64 }) return Result<SavedPage>.Failure("request.cursor_invalid", "Actualiza la lista para continuar.");
        return Result<SavedPage>.Success(await store.ListAsync(accountId, kind == "all" ? null : kind, cursor, limit, ct));
    }
}

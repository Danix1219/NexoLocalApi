using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Engagement.Domain.Orders;

namespace Nexo.Engagement.Application.Orders;

public sealed class OrderService(IOrderStore store, IOrderProviderAccess providers, IOrderAddressProtection addresses,
    IOrderSafety safety, TimeProvider clock)
{
    public async Task<Result<OrderResult>> CreateAsync(Guid client, Guid? requestId, string? idempotencyKey,
        CreateOrderBody? body, CancellationToken ct)
    {
        if (body == null || !Guid.TryParse(idempotencyKey, out var key) ||
            OrderRules.Validate(body.Origin, body.Description, body.Attention, body.Address, body.AddressNote,
                body.Location?.Lat, body.Location?.Lng) is not null ||
            (body.RequestId is not null && (body.RequestId != requestId || body.Origin != "post")) ||
            (body.Origin == "post" && body.RequestId is null) ||
            (body.Origin == "direct" && body.RequestId != null)) return Fail<OrderResult>(NexoCodes.ValidationFailed);
        var provider = await providers.GetAsync(body.ProviderId, ct);
        if (provider is not { Published: true }) return Fail<OrderResult>(NexoCodes.ProviderNotFound);
        if (provider.OwnerId == client) return Fail<OrderResult>(NexoCodes.ValidationFailed);
        if (body.Attention == "home" && provider.Attention is not ("home" or "both") ||
            body.Attention == "shop" && provider.Attention is not ("shop" or "both")) return Fail<OrderResult>(NexoCodes.OrderAttentionNotSupported);
        var moderated = await safety.ModerateAsync(body.Description.Trim(), ct);
        if (moderated.IsFailure) return Result<OrderResult>.Failure(moderated.Error!);
        if (!moderated.Value) return Fail<OrderResult>(NexoCodes.ContentInappropriate);
        string? cipher = null;
        OrderAddress? privateAddress = body.Attention == "home" ? new(body.Address!.Trim(), body.AddressNote?.Trim(), new(body.Location!.Lat, body.Location.Lng)) : null;
        if (privateAddress != null) cipher = await addresses.EncryptAsync(privateAddress, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var draft = new OrderDraft(Guid.CreateVersion7(), client, body.ProviderId, provider.OwnerId, body.RequestId, body.Origin,
            body.Description.Trim(), body.Attention, provider.Zone, cipher, null, now);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body))));
        var created = await store.CreateAsync(draft, key, hash, ct);
        if (created == null) return Fail<OrderResult>(NexoCodes.OrderActiveExists);
        var sameRequest = created.Id != draft.Id;
        return Result<OrderResult>.Success(new(new { orderId = created.Id, status = created.Status,
            statusText = "Esperando respuesta", providerId = created.ProviderId, providerName = provider.Name,
            requestId = created.RequestId, origin = created.Origin, attention = created.Attention,
            placeZone = created.PlaceZone, createdAt = created.CreatedAt, version = created.Version,
            allowedActions = new[] { "cancel", "changeAddress", "message" } }, sameRequest ? 200 : 201));
    }

    public async Task<Result<object>> ListAsync(Guid actor, string role, string status, string? cursor, int limit, CancellationToken ct)
    {
        if (role is not ("client" or "provider") || status is not ("pending" or "accepted" or "rejected" or "cancelled" or "resolved" or "active" or "all") ||
            limit is < 1 or > 50 || cursor is { Length: > 64 }) return Fail<object>(NexoCodes.ValidationFailed);
        Guid? providerId = role == "provider" ? await providers.ProviderForAccountAsync(actor, ct) : null;
        if (role == "provider" && providerId == null) return Fail<object>(NexoCodes.ProviderProfileRequired);
        Guid? before = null;
        if (cursor is { Length: > 0 } && !Guid.TryParse(cursor, out var parsed)) return Fail<object>(NexoCodes.RequestCursorInvalid);
        else if (cursor is { Length: > 0 }) before = Guid.Parse(cursor);
        var page = await store.ListAsync(actor, providerId, role, status, before, limit + 1, ct);
        var selected = page.Items.Take(limit).ToArray();
        var items = new List<object>(selected.Length);
        foreach (var o in selected)
        {
            var pendingHidden = o.Status == "pending";
            var counterpart = role == "client" ? await providers.GetAsync(o.ProviderId, ct) : null;
            var client = role == "provider" ? await providers.GetClientAsync(o.ClientId, ct) : null;
            items.Add(new { orderId = o.Id, status = o.Status,
                statusText = StatusText(o.Status, role), role, counterpart = new { accountId = role == "client" ? o.ProviderId : o.ClientId,
                    displayName = pendingHidden ? "" : counterpart?.Name ?? client?.Name ?? "Persona", photoUrl = client?.PhotoUrl, kind = role == "client" ? "provider" : "client" },
                category = o.Category, descriptionExcerpt = o.Description.Length > 200 ? o.Description[..200] : o.Description,
                attention = o.Attention, placeZone = o.PlaceZone, address = (string?)null,
                agreedPrice = o.PriceCents is { } cents ? new { amountCents = cents, text = $"${cents / 100m:0.00}", includes = o.PriceIncludes } : null,
                requestId = o.RequestId, conversationId = (Guid?)null, ago = Ago(o.CreatedAt), createdAt = o.CreatedAt,
                allowedActions = Actions(o.Status, role) });
        }
        return Result<object>.Success(new { items, nextCursor = page.Items.Length > limit && selected.Length > 0 ? selected[^1].Id.ToString("D") : null,
            hasMore = page.Items.Length > limit, counts = page.Counts });
    }

    public async Task<Result<object>> DetailAsync(Guid actor, Guid id, CancellationToken ct)
    {
        var order = await store.FindAsync(id, actor, ct);
        if (order == null) return Fail<object>(NexoCodes.ResourceNotFound);
        var role = order.ClientId == actor ? "client" : "provider";
        var canSee = role == "client" || order.Status is "accepted" or "resolved" || order.Status == "cancelled" && order.AcceptedAt != null;
        var plaintext = canSee && order.AddressCipher != null ? await addresses.DecryptAsync(order.AddressCipher, ct) : null;
        var timeline = await store.EventsAsync(id, ct);
        var provider = role == "client" ? await providers.GetAsync(order.ProviderId, ct) : null;
        var client = role == "provider" ? await providers.GetClientAsync(order.ClientId, ct) : null;
        return Result<object>.Success(new { orderId = id, status = order.Status, statusText = StatusText(order.Status, role), role,
            counterpart = new { accountId = role == "client" ? order.ProviderId : order.ClientId,
                displayName = order.Status == "pending" ? "" : provider?.Name ?? client?.Name ?? "Persona", photoUrl = client?.PhotoUrl, kind = role == "client" ? "provider" : "client", canCopyPhone = false },
            category = order.Category, description = order.Description, attention = order.Attention, placeZone = order.PlaceZone,
            address = plaintext?.Address, addressNote = plaintext?.Note, location = plaintext?.Point,
            agreedPrice = order.PriceCents is { } cents ? new { amountCents = cents, text = $"${cents / 100m:0.00}", includes = order.PriceIncludes } : null,
            requestId = order.RequestId, conversationId = (Guid?)null,
            timeline = timeline.Select(e => new { version = e.Version, type = e.Type, actor = e.ActorId == order.ClientId ? "client" : "provider", at = e.At }),
            version = order.Version, allowedActions = Actions(order.Status, role), canReview = false });
    }

    public async Task<Result<object>> TransitionAsync(Guid actor, Guid? providerId, Guid id, string target,
        int? expectedVersion, TransitionBody? body, CancellationToken ct)
    {
        var existing = await store.FindAsync(id, actor, ct);
        if (existing == null && (providerId == null || (existing = await store.FindAsync(id, providerId.Value, ct)) == null))
            return Fail<object>(NexoCodes.ResourceNotFound);
        var role = existing.ClientId == actor ? "client" : "provider";
        if (target is "accepted" or "rejected" or "resolved" && role != "provider") return Fail<object>(NexoCodes.ResourceNotFound);
        if (target == "cancelled" && !ServiceOrder.CanTransition(existing.Status, target)) return Fail<object>(NexoCodes.OrderInvalidTransition);
        var priceCents = body?.AgreedPrice?.AmountCents ?? body?.FinalPrice?.AmountCents;
        var priceIncludes = body?.AgreedPrice?.Includes ?? body?.FinalPrice?.Includes;
        if (priceCents is { } price && !OrderRules.ValidPrice(price)) return Fail<object>(NexoCodes.OrderPriceOutOfRange);
        foreach (var text in new[] { priceIncludes, body?.Note, body?.Reason }.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var check = await safety.ModerateAsync(text!, ct);
            if (check.IsFailure) return Result<object>.Failure(check.Error!);
            if (!check.Value) return Fail<object>(NexoCodes.ContentInappropriate);
        }
        var now = clock.GetUtcNow().UtcDateTime;
        var mutation = new OrderMutation(target, expectedVersion ?? existing.Version, now, role, priceCents,
            priceIncludes, body?.Reason, body?.Note);
        var changed = await store.TransitionAsync(id, actor, providerId, target, expectedVersion, mutation, ct);
        if (changed.NotFound) return Fail<object>(NexoCodes.ResourceNotFound);
        if (changed.VersionConflict) return Fail<object>(NexoCodes.OrderVersionConflict);
        if (changed.InvalidTransition) return Fail<object>(NexoCodes.OrderInvalidTransition);
        if (changed.Order == null) return Fail<object>(NexoCodes.ServerDependencyUnavailable);
        var releasedAddress = target == "accepted" && changed.Order.AddressCipher != null
            ? await addresses.DecryptAsync(changed.Order.AddressCipher, ct) : null;
        return Result<object>.Success(new { orderId = id, status = changed.Order.Status, version = changed.Order.Version,
            changedAt = now, acceptedAt = target == "accepted" ? now : (DateTime?)null,
            rejectedAt = target == "rejected" ? now : (DateTime?)null,
            cancelledAt = target == "cancelled" ? now : (DateTime?)null,
            resolvedAt = target == "resolved" ? now : (DateTime?)null,
            cancelledBy = target == "cancelled" ? role : null,
            clientCanReview = target == "resolved", requestReopened = false,
            address = releasedAddress?.Address, addressNote = releasedAddress?.Note, location = releasedAddress?.Point,
            agreedPrice = changed.Order.PriceCents is { } cents ? new { amountCents = cents, text = $"${cents / 100m:0.00}", includes = changed.Order.PriceIncludes } : null,
            allowedActions = Actions(changed.Order.Status, role) });
    }

    public async Task<Result<object>> ChangeAddressAsync(Guid actor, Guid id, int? expectedVersion, ChangeAddressBody? body, CancellationToken ct)
    {
        if (body == null || OrderRules.Validate("direct", new string('x', 10), "home", body.Address,
            body.AddressNote, body.Location?.Lat, body.Location?.Lng) is not null) return Fail<object>(NexoCodes.ValidationFailed);
        var existing = await store.FindAsync(id, actor, ct);
        if (existing == null) return Fail<object>(NexoCodes.ResourceNotFound);
        if (existing.Attention != "home") return Fail<object>(NexoCodes.ValidationFailed);
        var address = new OrderAddress(body.Address.Trim(), body.AddressNote?.Trim(), new(body.Location!.Lat, body.Location.Lng));
        var cipher = await addresses.EncryptAsync(address, ct);
        var result = await store.ChangeAddressAsync(id, actor, expectedVersion, address, cipher, existing.PlaceZone, ct);
        if (result.NotFound) return Fail<object>(NexoCodes.ResourceNotFound);
        if (result.VersionConflict) return Fail<object>(NexoCodes.OrderVersionConflict);
        if (result.InvalidTransition) return Fail<object>(NexoCodes.OrderInvalidTransition);
        return Result<object>.Success(new { orderId = id, status = result.Order!.Status, version = result.Order.Version,
            placeZone = result.Order.PlaceZone, address = address.Address, addressNote = address.Note, location = address.Point,
            zoneChanged = false, providerNotified = false });
    }

    private static string StatusText(string s, string role) => s switch { "pending" => role == "provider" ? "Esperando tu respuesta" : "Esperando respuesta", "accepted" => "Aceptado", "rejected" => "Rechazado", "cancelled" => "Cancelado", "resolved" => "Resuelto", _ => s };
    private string Ago(DateTime createdAt)
    {
        var elapsed = clock.GetUtcNow().UtcDateTime - createdAt;
        return elapsed.TotalHours < 1 ? "hace un momento" : elapsed.TotalDays < 1 ? $"hace {(int)elapsed.TotalHours} h" : $"hace {(int)elapsed.TotalDays} d";
    }
    private static string[] Actions(string s, string role) => (s, role) switch
    { ("pending", "client") => ["cancel", "changeAddress", "message"], ("pending", "provider") => ["accept", "reject", "message"],
      ("accepted", "client") => ["cancel", "changeAddress", "message"], ("accepted", "provider") => ["resolve", "cancel", "message", "copyPhone"],
      ("resolved", "client") => ["message"], ("resolved", "provider") => ["message", "copyPhone"], _ => [] };
    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación del pedido.");
    public sealed record CreateOrderBody(Guid ProviderId, Guid? RequestId, string Origin, string Description, string Attention, string? Address, string? AddressNote, OrderLocation? Location);
    public sealed record OrderPriceBody(int? AmountCents = null, string? Includes = null);
    public sealed record TransitionBody(OrderPriceBody? AgreedPrice = null, string? Reason = null, string? Note = null, OrderPriceBody? FinalPrice = null);
    public sealed record ChangeAddressBody(string Address, string? AddressNote, OrderLocation? Location);
    public sealed record OrderLocation(double Lat, double Lng);
}

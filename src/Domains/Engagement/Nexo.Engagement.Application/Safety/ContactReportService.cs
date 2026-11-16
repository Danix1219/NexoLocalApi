using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Engagement.Application.Conversations;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Engagement.Application.Safety;

public sealed class ContactReportService(IContactReportStore store, IContactDirectory directory, IConversationStore conversations,
    IOrderProviderAccess providers, IOrderSafety safety, TimeProvider clock)
{
    public const int PhoneDailyLimit = 40;
    public const int ReportDailyLimit = 20;
    private static readonly string[] Contexts = ["profile", "chat", "order", "interested"];
    private static readonly string[] ReportTargets = ["provider", "work", "request", "conversation", "review"];
    private static readonly string[] Categories = ["scam", "inappropriateContent", "harassment", "fakeProfile", "notAService", "other"];

    /// <summary>The only way a phone number leaves the API: deliberate, limited (40/day) and logged.</summary>
    public async Task<Result<object>> RevealPhoneAsync(Guid me, PhoneBody? body, CancellationToken ct)
    {
        if (body?.TargetId is not { } targetId || body.TargetType is not ("provider" or "client") ||
            body.Context is { } c && !Contexts.Contains(c)) return Fail<object>(NexoCodes.ValidationFailed);
        var now = clock.GetUtcNow().UtcDateTime;
        var used = await store.PhoneAccessesSinceAsync(me, now.AddDays(-1), ct);
        if (used >= PhoneDailyLimit) return Fail<object>(NexoCodes.ContactDailyLimit);

        Guid account; string name;
        if (body.TargetType == "provider")
        {
            var provider = await directory.ProviderAsync(targetId, ct);
            if (provider == null) return Fail<object>(NexoCodes.ResourceNotFound);
            if (provider.AccountId == me) return Fail<object>(NexoCodes.ContactNotAllowed);
            if (!provider.CallEnabled) return Fail<object>(NexoCodes.ContactPhoneNotAvailable);
            (account, name) = (provider.AccountId, provider.Name);
        }
        else
        {
            // A provider sees a client's number only once that client's order was accepted.
            var myProfile = await providers.ProviderForAccountAsync(me, ct);
            var orders = myProfile is { } p ? await conversations.PairOrdersAsync([(targetId, p)], ["accepted", "resolved"], ct) : [];
            if (orders.Length == 0) return Fail<object>(NexoCodes.ContactNotAllowed);
            (account, name) = (targetId, await directory.ClientNameAsync(targetId, ct) ?? "Cliente");
        }

        var phone = await directory.PhoneAsync(account, "contact.copyPhone:" + (body.Context ?? "profile"), ct);
        if (phone == null) return Fail<object>(NexoCodes.ContactPhoneNotAvailable);
        await store.LogPhoneAsync(me, account, body.Context ?? "profile", now, ct);
        return Result<object>.Success(new
        {
            phone,
            displayPhone = DisplayPhone(phone),
            targetName = name,
            remainingToday = Math.Max(0, PhoneDailyLimit - used - 1),
            loggedAt = now
        });
    }

    /// <summary>"+527715550100" → "771 555 0100".</summary>
    public static string DisplayPhone(string e164) => e164 is { Length: 13 } && e164.StartsWith("+52", StringComparison.Ordinal)
        ? $"{e164[3..6]} {e164[6..9]} {e164[9..]}" : e164;

    /// <summary>Accepted for human review (202). A harsh-worded reason is flagged, never rejected: losing a fraud report is worse.</summary>
    public async Task<Result<object>> ReportAsync(Guid me, string? idempotencyKey, ReportBody? body, CancellationToken ct)
    {
        var reason = body?.Reason?.Trim();
        if (body?.TargetId is not { } targetId || !Guid.TryParse(idempotencyKey, out var key) || !ReportTargets.Contains(body.TargetType) ||
            !Categories.Contains(body.Category) || reason is not { Length: >= 10 and <= 300 }) return Fail<object>(NexoCodes.ValidationFailed);

        var previous = await store.ReportOfAsync(me, body.TargetType!, targetId, ct);
        if (previous != null)
            return previous.IdempotencyKey == key ? Result<object>.Success(Accepted(previous)) : Result<object>.Failure(NexoCodes.ReportAlreadyReported,
                $"Ya reportaste esto el {previous.CreatedAt:dd/MM/yyyy}.");

        var owner = await OwnerAsync(me, body.TargetType!, targetId, ct);
        if (owner == null) return Fail<object>(NexoCodes.ResourceNotFound);
        if (owner == me) return Fail<object>(NexoCodes.ReportOwnContent);
        if (body.EvidenceConversationId is { } evidence && await conversations.FindAsync(evidence, me, ct) == null)
            return Fail<object>(NexoCodes.ConversationNotParticipant);

        var now = clock.GetUtcNow().UtcDateTime;
        if (await store.ReportsSinceAsync(me, now.AddDays(-1), ct) >= ReportDailyLimit) return Fail<object>(NexoCodes.RateLimitExceeded);
        var moderation = await safety.ModerateAsync(reason, ct);
        bool? flagged = moderation.IsSuccess ? !moderation.Value : moderation.Error!.Code == NexoCodes.ContentInappropriate ? true : null;

        var report = new StoredReport(Guid.CreateVersion7(), me, body.TargetType!, targetId, key, now);
        if (!await store.InsertReportAsync(report, body.Category!, reason, flagged, body.EvidenceConversationId, ct))
            return Result<object>.Failure(NexoCodes.ReportAlreadyReported, "Ya reportaste esto.");
        return Result<object>.Success(Accepted(report));
    }

    /// <summary>Who owns the reported thing; for a conversation, the other participant (and only if the reporter is in it).</summary>
    private async Task<Guid?> OwnerAsync(Guid me, string type, Guid id, CancellationToken ct) => type switch
    {
        "conversation" => await conversations.FindAsync(id, me, ct) is { } c ? (c.IsClient(me) ? c.ProviderAccountId : c.ClientId) : null,
        "review" => await store.ReviewAuthorAsync(id, ct),
        _ => await directory.OwnerAsync(type, id, ct)
    };

    private static object Accepted(StoredReport r) => new
    {
        reportId = r.Id,
        status = "received",
        message = "Recibimos tu reporte. Lo revisará una persona del equipo.",
        createdAt = r.CreatedAt,
        alsoBlocked = false
    };

    private static Result<T> Fail<T>(string code) => Result<T>.Failure(code, "No se pudo completar la operación.");
    public sealed record PhoneBody(Guid? TargetId = null, string? TargetType = null, string? Context = null);
    public sealed record ReportBody(string? TargetType = null, Guid? TargetId = null, string? Category = null, string? Reason = null, Guid? EvidenceConversationId = null);
}

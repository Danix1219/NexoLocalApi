namespace Nexo.Engagement.Application.Safety;

public sealed record ContactTarget(Guid AccountId, string Name, bool CallEnabled);
public sealed record StoredReport(Guid Id, Guid ReporterId, string TargetType, Guid TargetId, Guid IdempotencyKey, DateTime CreatedAt);

/// <summary>Phone access log and human-review reports, both owned by Engagement.</summary>
public interface IContactReportStore
{
    Task<int> PhoneAccessesSinceAsync(Guid actor, DateTime since, CancellationToken ct);
    Task LogPhoneAsync(Guid actor, Guid targetAccount, string context, DateTime at, CancellationToken ct);
    Task<int> ReportsSinceAsync(Guid reporter, DateTime since, CancellationToken ct);
    Task<StoredReport?> ReportOfAsync(Guid reporter, string targetType, Guid targetId, CancellationToken ct);
    /// <summary>False when the reporter already reported that target (unique index).</summary>
    Task<bool> InsertReportAsync(StoredReport report, string category, string reason, bool? reasonFlagged, Guid? evidenceConversationId, CancellationToken ct);
    Task<Guid?> ReviewAuthorAsync(Guid reviewId, CancellationToken ct);
}

/// <summary>Other modules: provider call channel (Discovery), the phone itself (Identity) and content owners.</summary>
public interface IContactDirectory
{
    Task<ContactTarget?> ProviderAsync(Guid providerId, CancellationToken ct);
    Task<string?> ClientNameAsync(Guid accountId, CancellationToken ct);
    Task<string?> PhoneAsync(Guid accountId, string reason, CancellationToken ct);
    /// <summary>Owner account of a public provider/work/request, or null when it does not exist.</summary>
    Task<Guid?> OwnerAsync(string kind, Guid id, CancellationToken ct);
}

public sealed class ContactReportUnavailableException(Exception? inner = null) : Exception("Contact and reports are temporarily unavailable.", inner);

namespace Nexo.Engagement.Application.Conversations;

/// <summary>Live fan-out to a user's open connections. Only broadcasts what the HTTP endpoint already stored:
/// if it fails or nobody is connected, the app recovers with `?after=` or its fallback refresh.</summary>
public interface IChatRealtime
{
    Task SendAsync(Guid account, string method, object payload, CancellationToken ct);
}

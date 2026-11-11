namespace Nexo.Engagement.Domain.Orders;

public static class ServiceOrder
{
    public const string Pending = "pending";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
    public const string Resolved = "resolved";

    public static bool IsActive(string status) => status is Pending or Accepted;
    public static bool CanTransition(string from, string to) => (from, to) switch
    {
        (Pending, Accepted or Rejected or Cancelled) => true,
        (Accepted, Resolved or Cancelled) => true,
        _ => false
    };
}

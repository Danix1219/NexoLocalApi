namespace Nexo.Engagement.Domain.Orders;

public static class OrderRules
{
    public static string? Validate(string? origin, string? description, string? attention, string? address,
        string? addressNote, double? lat, double? lng)
    {
        if (origin is not ("post" or "direct") || string.IsNullOrWhiteSpace(description) || description.Trim().Length is < 10 or > 600 ||
            description.Any(char.IsControl) || attention is not ("home" or "shop") || addressNote?.Length > 120 ||
            (attention == "home" && (string.IsNullOrWhiteSpace(address) || address.Length > 300 || lat is null || lng is null)) ||
            (attention == "shop" && (address != null || lat != null || lng != null)) ||
            lat is { } y && (!double.IsFinite(y) || y is < 14 or > 33) ||
            lng is { } x && (!double.IsFinite(x) || x is < -119 or > -86)) return "validation_failed";
        return null;
    }

    public static bool ValidPrice(int? cents) => cents is null or >= 0 and <= 99_999_900;
}

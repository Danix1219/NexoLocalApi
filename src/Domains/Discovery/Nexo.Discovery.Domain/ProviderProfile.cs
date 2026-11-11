using System.Globalization;
using System.Security.Cryptography;

namespace Nexo.Discovery.Domain;

public sealed record ProviderPoint(double Lat, double Lng)
{
    public bool IsValid => double.IsFinite(Lat) && double.IsFinite(Lng) && Lat is >= -90 and <= 90 && Lng is >= -180 and <= 180;
    public double DistanceKm(ProviderPoint other)
    {
        static double R(double d) => d * Math.PI / 180;
        var a = Math.Pow(Math.Sin(R(other.Lat - Lat) / 2), 2) + Math.Cos(R(Lat)) * Math.Cos(R(other.Lat)) * Math.Pow(Math.Sin(R(other.Lng - Lng) / 2), 2);
        return 6371 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
    public ProviderPoint Blur()
    {
        var bearing = RandomNumberGenerator.GetInt32(360000) * Math.PI / 180000;
        var distance = RandomNumberGenerator.GetInt32(500, 801) / 6371000d;
        var lat = Lat * Math.PI / 180;
        var next = Math.Asin(Math.Sin(lat) * Math.Cos(distance) + Math.Cos(lat) * Math.Sin(distance) * Math.Cos(bearing));
        var lng = Lng * Math.PI / 180 + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance) * Math.Cos(lat), Math.Cos(distance) - Math.Sin(lat) * Math.Sin(next));
        return new(next * 180 / Math.PI, (lng * 180 / Math.PI + 540) % 360 - 180);
    }
}
public sealed record ProviderZone(string Label, ProviderPoint Point);
public sealed record ProviderDay(string Open, string Close);
public sealed record ProviderSchedule(ProviderDay?[] Days, bool Urgent = false)
{
    public bool IsValid => Days is { Length: 7 } && Days.Any(d => d != null) && Days.All(d => d is null ||
        (TimeOnly.TryParseExact(d.Open, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
         TimeOnly.TryParseExact(d.Close, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) && end > start));
    public bool IsOpen(DateTime utc)
    {
        if (!IsValid) return false;
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City"));
        var day = Days[((int)local.DayOfWeek + 6) % 7];
        return day != null && TimeOnly.FromDateTime(local) >= TimeOnly.ParseExact(day.Open, "HH:mm", CultureInfo.InvariantCulture) &&
            TimeOnly.FromDateTime(local) < TimeOnly.ParseExact(day.Close, "HH:mm", CultureInfo.InvariantCulture);
    }
}
public sealed record ProviderVisit(string Mode = "hidden", int? AmountCents = null);
public sealed record ProviderPrice(string Service, int? FromCents, string Unit = "from", string Suffix = "");
public sealed record ProviderChannels(bool Chat = true, bool Call = false);
public sealed record ProviderImage(string Url, string? ThumbUrl, int W, int H);
public sealed record ProviderProfile
{
    public const string CompletenessPolicyVersion = "provider-completeness/1";
    public Guid ProviderId { get; init; }
    public string Kind { get; init; } = "trade";
    public string Name { get; init; } = "";
    public string? Owner { get; init; }
    public string? Sector { get; init; }
    public string? Activity { get; init; }
    public string[] Trades { get; init; } = [];
    public string[] Categories { get; init; } = [];
    public string[] Specialties { get; init; } = [];
    public string Bio { get; init; } = "";
    public int Experience { get; init; }
    public string Crew { get; init; } = "alone";
    public int? TeamSize { get; init; }
    public string Attention { get; init; } = "home";
    public ProviderPoint? Location { get; init; }
    public ProviderZone? PublicZone { get; init; }
    public int RadiusKm { get; init; } = 10;
    public ProviderSchedule? Schedule { get; init; }
    public ProviderVisit Visit { get; init; } = new();
    public ProviderPrice[] Prices { get; init; } = [];
    public string[] Payment { get; init; } = [];
    public string? ResponseTime { get; init; }
    public string? Warranty { get; init; }
    public bool Invoice { get; init; }
    public string? Web { get; init; }
    public ProviderChannels Channels { get; init; } = new();
    public string? PhotoUrl { get; init; }
    public string? CoverUrl { get; init; }
    public ProviderImage[] Portfolio { get; init; } = [];
    public int Jobs { get; init; }
    public int Recs { get; init; }
    public double RatingAverage { get; init; }
    public int RatingCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Completeness => Math.Min(100, 35 + (PhotoUrl != null ? 7 : 0) + (Schedule?.IsValid == true ? 14 : 0) +
        (Prices.Length > 0 ? 14 : 0) + (Jobs > 0 ? 14 : 0) + (Payment.Length > 0 || Warranty != null ? 9 : 0) + (CoverUrl != null ? 7 : 0));
}

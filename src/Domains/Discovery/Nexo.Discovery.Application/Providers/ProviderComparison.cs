using System.Globalization;
using System.Text.Json;

namespace Nexo.Discovery.Application.Providers;

public static class ProviderComparison
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-MX");
    public static object Build(List<Dictionary<string, object?>> profiles)
    {
        var p = profiles.Select(p => JsonSerializer.SerializeToElement(p, ProviderService.Json)).ToArray();
        var rows = new List<object>();
        string Text(int i, string field) => p[i].TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : "—";
        double? Number(int i, string field) => p[i].TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
        void Row(string key, string label, string[] values, double?[]? numbers = null, bool lower = false)
        {
            int? best = null;
            if (numbers is { Length: 2 } && numbers[0] != null && numbers[1] != null && numbers[0] != numbers[1])
                best = (lower ? numbers[0] < numbers[1] : numbers[0] > numbers[1]) == true ? 0 : 1;
            rows.Add(new { key, label, values, best });
        }
        string[] Values(Func<int, string> fn) => [fn(0), fn(1)];
        double?[] Numbers(Func<int, double?> fn) => [fn(0), fn(1)];
        double? Min(int i) => p[i].GetProperty("minPrice") is var m && m.ValueKind == JsonValueKind.Object ? m.GetProperty("fromCents").GetDouble() : null;
        double? Visit(int i) => p[i].GetProperty("visit").GetProperty("mode").GetString() switch { "free" => 0, "paid" => p[i].GetProperty("visit").GetProperty("amountCents").GetDouble(), _ => null };
        string Money(double? cents) => cents == null ? "A cotizar" : (cents.Value / 100).ToString(cents.Value % 100 == 0 ? "C0" : "C2", Culture);
        Row("rating", "Calificacion", Values(i => $"{p[i].GetProperty("rating").GetProperty("avg").GetDouble().ToString("0.0", Culture)} ({p[i].GetProperty("rating").GetProperty("count").GetInt32()})"), Numbers(i => p[i].GetProperty("rating").GetProperty("count").GetInt32() > 0 ? p[i].GetProperty("rating").GetProperty("avg").GetDouble() : null));
        Row("recs", "Recomendaciones", Values(i => Number(i, "recs")?.ToString(Culture) ?? "—"), Numbers(i => Number(i, "recs")));
        Row("minPrice", "Precio desde", Values(i => Money(Min(i))), Numbers(Min), true);
        Row("visit", "Costo de visita", Values(i => Visit(i) == 0 ? "Sin costo" : Money(Visit(i))), Numbers(Visit), true);
        Row("distance", "Distancia", Values(i => Number(i, "distanceKm") is { } km ? $"{km.ToString("0.0", Culture)} km" : "—"), Numbers(i => Number(i, "distanceKm")), true);
        Row("attention", "Tipo de atencion", Values(i => Text(i, "attention") switch { "home" => "A domicilio", "shop" => "En local", _ => "Local y domicilio" }));
        Row("status", "Ahora", Values(i => p[i].GetProperty("status").GetProperty("text").GetString()!), Numbers(i => p[i].GetProperty("status").GetProperty("open").GetBoolean() ? 1 : 0));
        bool Urgent(int i) => p[i].GetProperty("schedule") is var schedule && schedule.ValueKind == JsonValueKind.Object && schedule.GetProperty("urgent").GetBoolean();
        Row("urgent", "Atiende urgencias", Values(i => Urgent(i) ? "Si" : "No"), Numbers(i => Urgent(i) ? 1 : 0));
        double? Response(int i) => Text(i, "responseTime") switch { "sameDay" => 0, "h24" => 1, "h48" => 2, "week" => 3, _ => null };
        Row("responseTime", "Responde", Values(i => Text(i, "responseTime") switch { "sameDay" => "El mismo dia", "h24" => "En 24 h", "h48" => "En 48 h", "week" => "En la semana", _ => "Sin publicar" }), Numbers(Response), true);
        Row("warranty", "Garantia declarada", Values(i => Text(i, "warranty")));
        Row("payment", "Formas de pago", Values(i => string.Join(", ", p[i].GetProperty("payment").EnumerateArray().Select(v => v.GetString() switch { "cash" => "Efectivo", "transfer" => "Transferencia", "card" => "Tarjeta", "deposit" => "Deposito", _ => "Otro" }))));
        Row("emailVerified", "Correo confirmado", Values(i => p[i].GetProperty("emailVerified").GetBoolean() ? "Si" : "No"));
        Row("experience", "Experiencia", Values(i => $"{Number(i, "experience")} anos"), Numbers(i => Number(i, "experience")));
        Row("jobs", "Trabajos publicados", Values(i => Number(i, "jobs")?.ToString(Culture) ?? "0"), Numbers(i => Number(i, "jobs")));
        return new { providers = p.Select(i => new { providerId = i.GetProperty("providerId").GetGuid(), name = i.GetProperty("name").GetString(), kind = i.GetProperty("kind").GetString(), photoUrl = i.GetProperty("photoUrl") }), rows };
    }
}

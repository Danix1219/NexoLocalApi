using System.Globalization;
using System.Text;
using Nexo.Common.Errors;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Application.Providers;

public static class ProviderRules
{
    public static readonly Dictionary<string, string[]> Specialties = new()
    {
        ["plomeria"] = ["fugas", "calentadores", "destapes", "tinacos", "banos"],
        ["electricidad"] = ["residencial", "comercial", "tableros", "cortos", "iluminacion"],
        ["carpinteria"] = ["cocinas", "puertas", "muebles", "reparaciones", "restauracion"],
        ["pintura"] = ["interiores", "exteriores", "impermeabilizado", "acabados", "muros"],
        ["jardineria"] = ["mantenimiento", "poda", "diseno", "riego", "pasto"],
        ["cerrajeria"] = ["aperturas", "chapas", "llaves", "seguridad", "automotriz"],
        ["otros"] = []
    };
    private static readonly Dictionary<string, string[]> Keywords = new()
    {
        ["plomeria"] = ["plomer", "fontaner", "fuga", "tuber", "calentador", "drenaje"],
        ["electricidad"] = ["electric", "cable", "circuit", "tablero", "iluminacion"],
        ["carpinteria"] = ["carpinter", "madera", "mueble", "closet"],
        ["pintura"] = ["pintur", "pintor", "impermeabil"],
        ["jardineria"] = ["jardin", "poda", "pasto", "riego"],
        ["cerrajeria"] = ["cerrajer", "chapa", "llave", "cerradura"]
    };
    public static string[] Infer(string text)
    {
        var normalized = string.Concat(text.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
        var result = Keywords.Where(k => k.Value.Any(w => normalized.Contains(w, StringComparison.Ordinal))).Take(3).Select(k => k.Key).ToArray();
        return result.Length == 0 ? ["otros"] : result;
    }
    public static string? Validate(ProviderProfile p, string section, ProviderAccountState account)
    {
        static bool Text(string? s, int min, int max) => s != null && s.Trim().Length >= min && s.Length <= max && !s.Any(char.IsControl);
        if (section is "all" or "basic")
        {
            if (p.Kind is not ("trade" or "business") || !Text(p.Name, 2, 80) || p.Name.All(char.IsDigit) || p.Experience is < 0 or > 70 ||
                p.Crew is not ("alone" or "withHelpers" or "team") || (p.TeamSize != null && (p.Kind != "business" || p.Crew != "team" || p.TeamSize is < 1 or > 200)) || (p.Kind == "trade" && (p.Owner != null || p.TeamSize != null || p.Crew == "team")) ||
                (p.Kind == "business" && (!Text(p.Owner, 2, 80) || (p.Crew == "team" && p.TeamSize is not (>= 1 and <= 200))))) return NexoCodes.ValidationFailed;
        }
        if (section is "all" or "services")
        {
            if (p.Bio == null || p.Bio.Trim().Length < 20) return NexoCodes.ProviderBioTooShort;
            if (!Text(p.Bio, 20, 600) || p.Categories == null || p.Categories.Length is < 1 or > 3 || p.Categories.Any(c => !Specialties.ContainsKey(c)) || p.Categories.Distinct().Count() != p.Categories.Length) return NexoCodes.ProviderCategoriesRequired;
            if (p.Trades == null || p.Trades.Length > 5 || p.Trades.Any(t => !Text(t, 1, 80)) || (p.Kind == "trade" && p.Trades.Length == 0) ||
                (p.Kind == "business" && (!Text(p.Sector, 1, 80) || !Text(p.Activity, 1, 80))) ||
                (p.Kind == "trade" && (p.Sector != null || p.Activity != null)) || p.Specialties == null || p.Specialties.Length > 5 ||
                p.Specialties.Distinct().Count() != p.Specialties.Length || p.Specialties.Any(s => !p.Categories.Any(c => Specialties[c].Contains(s)))) return NexoCodes.ValidationFailed;
        }
        if (section is "all" or "zone")
        {
            if (p.Location?.IsValid != true || p.Attention is not ("home" or "shop" or "both")) return NexoCodes.ValidationFailed;
            if (p.RadiusKm is not (0 or 2 or 5 or 10 or 20) || (p.RadiusKm == 0 && p.Attention != "shop")) return NexoCodes.ProviderRadiusInvalid;
        }
        if (section == "hours" && p.Schedule?.IsValid != true) return NexoCodes.ProviderScheduleInvalid;
        if (section == "prices")
        {
            if (p.Prices == null || p.Prices.Length > 3) return NexoCodes.ProviderPricesLimit;
            if (p.Visit == null || p.Visit.Mode is not ("hidden" or "free" or "paid") || (p.Visit.Mode == "paid" && p.Visit.AmountCents is not (>= 0 and <= 99999900)) ||
                p.Prices.Any(i => i == null || !Text(i.Service, 1, 60) || i.Unit is not ("from" or "quote") || (i.Unit == "from" && i.FromCents is not (>= 0 and <= 99999900)) || (i.Unit == "quote" && i.FromCents != null) || i.Suffix == null || i.Suffix.Length > 20)) return NexoCodes.ValidationFailed;
        }
        if (section == "terms")
        {
            if (p.Payment == null || p.Payment.Length > 5 || p.Payment.Distinct().Count() != p.Payment.Length || p.Payment.Any(s => s is not ("cash" or "transfer" or "card" or "deposit" or "other")) ||
                p.Channels == null || (p.ResponseTime != null && p.ResponseTime is not ("sameDay" or "h24" or "h48" or "week")) || (p.Warranty != null && !Text(p.Warranty, 0, 120)) ||
                (p.Web != null && (p.Kind != "business" || p.Web.Length > 200 || !Uri.TryCreate(p.Web, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length != 0)) || (p.Kind == "trade" && p.Invoice)) return NexoCodes.ValidationFailed;
            if (p.Channels.Call && !account.EmailVerified) return NexoCodes.AuthEmailNotVerified;
            if (p.Channels.Call && !account.HasPhone) return NexoCodes.PhoneInvalid;
        }
        return null;
    }
}

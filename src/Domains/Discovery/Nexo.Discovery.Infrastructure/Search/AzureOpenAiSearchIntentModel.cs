using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Discovery.Application.Directory;

namespace Nexo.Discovery.Infrastructure.Search;

/// <summary>Last resort when ML.NET is unsure: asks the AzureOpenAI__ deployment for one slug. Off without configuration.</summary>
public sealed class AzureOpenAiSearchIntentModel(HttpClient http, IConfiguration configuration, ILogger<AzureOpenAiSearchIntentModel> log) : ISearchIntentModel
{
    // shortcut: per-instance memory cache capped at 2,000 texts; move to Redis if several instances run or the hit rate matters.
    private static readonly ConcurrentDictionary<string, string?> Cache = new();

    public string Source => "ai";

    public async Task<string?> ClassifyAsync(string text, IReadOnlyList<string> slugs, CancellationToken ct)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var key = configuration["AzureOpenAI:ApiKey"];
        var deployment = configuration["AzureOpenAI:Deployment"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var baseUri) || baseUri.Scheme != "https" ||
            string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(deployment)) return null;
        var normalized = DirectoryCatalog.Normalize(text);
        if (Cache.TryGetValue(normalized, out var cached)) return cached;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3)); // search must stay fast; without an answer it just shows no results
            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(baseUri, $"/openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version=2024-10-21"));
            request.Headers.Add("api-key", key);
            request.Content = JsonContent.Create(new
            {
                messages = new object[]
                {
                    new { role = "system", content = "Clasifica la búsqueda de una app mexicana de servicios del hogar. " +
                        $"Responde solo JSON {{\"category\": \"<una de: {string.Join(", ", slugs)}, none>\"}}. Usa none si no es claramente un oficio." },
                    new { role = "user", content = text }
                },
                temperature = 0,
                max_tokens = 20,
                response_format = new { type = "json_object" }
            });
            using var response = await http.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            using var answer = JsonDocument.Parse(doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "{}");
            var slug = answer.RootElement.TryGetProperty("category", out var c) ? c.GetString() : null;
            slug = slug != null && slugs.Contains(slug) ? slug : null;
            if (Cache.Count < 2000) Cache[normalized] = slug;
            return slug;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException
                                       || e is OperationCanceledException && !ct.IsCancellationRequested)
        {
            log.LogWarning(e, "Clasificación de búsqueda con IA no disponible.");
            return null;
        }
    }
}

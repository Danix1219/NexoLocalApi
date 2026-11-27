using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Discovery.Application.Assist;

namespace Nexo.Discovery.Infrastructure.Assist;

/// <summary>Azure OpenAI chat completions over REST (no SDK). Off until AzureOpenAI__Endpoint, __ApiKey and __Deployment exist.</summary>
public sealed class AzureOpenAiAssistModel(HttpClient http, IConfiguration configuration, ILogger<AzureOpenAiAssistModel> log) : IAssistModel
{
    private const string ApiVersion = "2024-10-21";

    public async Task<AssistDraft?> DraftAsync(string kind, string category, string userText, int lengthLimit, CancellationToken ct)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var key = configuration["AzureOpenAI:ApiKey"];
        var deployment = configuration["AzureOpenAI:Deployment"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var baseUri) || baseUri.Scheme != "https" ||
            string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(deployment)) return null;

        var what = kind == "work" ? "la descripción de un trabajo ya realizado por un prestador" : "una solicitud de servicio de un cliente";
        var system = $$"""
            Eres el asistente de Nexo Local, una app mexicana para encontrar servicios del hogar.
            Redacta {{what}} de la categoría "{{category}}" a partir de lo que escribió la persona.
            Español de México, claro y amable, en primera persona. Máximo {{lengthLimit}} caracteres.
            No inventes datos (precios, fechas, marcas, direcciones, teléfonos). Si falta información, no la agregues.
            Responde solo JSON: {"title": "título de hasta 60 caracteres", "text": "el texto"}.
            """;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(baseUri, $"/openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version={ApiVersion}"));
            request.Headers.Add("api-key", key);
            request.Content = JsonContent.Create(new
            {
                messages = new object[] { new { role = "system", content = system }, new { role = "user", content = userText } },
                temperature = 0.4,
                max_tokens = 400,
                response_format = new { type = "json_object" }
            });
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                log.LogWarning("Azure OpenAI respondió {Status}.", (int)response.StatusCode);
                return null;
            }
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            var model = doc.RootElement.TryGetProperty("model", out var m) ? m.GetString() ?? deployment : deployment;
            using var draft = JsonDocument.Parse(content ?? "{}");
            var text = draft.RootElement.TryGetProperty("text", out var t) ? t.GetString() : null;
            var title = draft.RootElement.TryGetProperty("title", out var ti) ? ti.GetString() : null;
            return string.IsNullOrWhiteSpace(text) ? null : new AssistDraft(text.Trim(), title?.Trim(), model);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException
                                       || e is OperationCanceledException && !ct.IsCancellationRequested)
        {
            log.LogWarning(e, "Asistente no disponible.");
            return null;
        }
    }
}

/// <summary>One counter per account and day (`assist_usage`), expired by TTL after two days. Never stores the text.</summary>
public sealed class MongoAssistQuota([FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : IAssistQuota
{
    private readonly IMongoCollection<BsonDocument> usage = database.GetCollection<BsonDocument>("assist_usage");
    private bool indexed;

    public async Task<bool> TryConsumeAsync(Guid account, DateTime today, int limit, CancellationToken ct)
    {
        if (!indexed)
        {
            await usage.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("day"),
                new CreateIndexOptions { Name = "ttl_assist_usage", ExpireAfter = TimeSpan.FromDays(2) }), cancellationToken: ct);
            indexed = true;
        }
        var id = $"{account:N}:{today:yyyyMMdd}";
        // Conditional increment: under the limit it counts, at the limit nothing matches and the upsert hits the existing _id.
        try
        {
            await usage.UpdateOneAsync(new BsonDocument { ["_id"] = id, ["count"] = new BsonDocument("$lt", limit) },
                new BsonDocument { ["$inc"] = new BsonDocument("count", 1), ["$setOnInsert"] = new BsonDocument("day", today) },
                new UpdateOptions { IsUpsert = true }, ct);
            return true;
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey) { return false; }
    }
}

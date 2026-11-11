using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Common.Mongo;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Infrastructure.Providers;

public sealed class ProviderSafetyOptions
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
}
public sealed class ProviderSafety(HttpClient client, IOptions<ProviderSafetyOptions> options,
    [FromKeyedServices(NexoDatabaseKeys.Discovery)] IMongoDatabase database) : IProviderSafety
{
    private static readonly string[] Categories = ["Hate", "Sexual", "Violence", "SelfHarm"];
    public async Task<Result<bool>> ModerateAsync(string text, CancellationToken ct)
    {
        var config = options.Value;
        if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" || endpoint.UserInfo.Length != 0 || string.IsNullOrWhiteSpace(config.ApiKey)) return Unavailable<bool>();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, "/contentsafety/text:analyze?api-version=2024-09-01"));
            request.Headers.Add("Ocp-Apim-Subscription-Key", config.ApiKey);
            request.Content = JsonContent.Create(new { text, categories = Categories, outputType = "FourSeverityLevels" });
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return Unavailable<bool>();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!document.RootElement.TryGetProperty("categoriesAnalysis", out var analysis) || analysis.ValueKind != JsonValueKind.Array) return Unavailable<bool>();
            var found = new HashSet<string>(StringComparer.Ordinal);
            var rejected = false;
            foreach (var entry in analysis.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("category", out var category) || category.ValueKind != JsonValueKind.String ||
                    !Categories.Contains(category.GetString()) || !found.Add(category.GetString()!) || !entry.TryGetProperty("severity", out var severity) ||
                    !severity.TryGetInt32(out var number) || number is not (0 or 2 or 4 or 6)) return Unavailable<bool>();
                rejected |= number >= 2;
            }
            if (found.Count != 4) return Unavailable<bool>();
            return rejected ? Result<bool>.Failure(NexoCodes.ContentInappropriate, "El contenido no puede publicarse.") : Result<bool>.Success(true);
        }
        catch (HttpRequestException) { return Unavailable<bool>(); }
        catch (JsonException) { return Unavailable<bool>(); }
        catch (InvalidOperationException) { return Unavailable<bool>(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Unavailable<bool>(); }
    }
    public async Task<Result<ProviderImage>> ImageAsync(Guid ownerId, Guid uploadId, string purpose, CancellationToken ct)
    {
        var doc = await database.GetCollection<BsonDocument>("uploads").Find(new BsonDocument
        {
            ["_id"] = new BsonBinaryData(uploadId, GuidRepresentation.Standard), ["ownerId"] = new BsonBinaryData(ownerId, GuidRepresentation.Standard),
            ["status"] = "approved", ["purpose"] = purpose
        }).FirstOrDefaultAsync(ct);
        if (doc == null) return Result<ProviderImage>.Failure(NexoCodes.UploadNotFound, "La imagen no esta disponible.");
        static string? Url(BsonDocument d, string name) => d.TryGetValue(name, out var v) && v.IsString && v.AsString.Length <= 200 &&
            Uri.TryCreate(v.AsString, UriKind.Absolute, out var u) && (u.Scheme == "https" || u.Scheme == "http" && u.IsLoopback && u.AbsolutePath.StartsWith("/v1/discovery/media/", StringComparison.Ordinal)) && u.UserInfo.Length == 0 ? v.AsString : null;
        var url = Url(doc, "url");
        if (url == null) return Unavailable<ProviderImage>();
        if (!doc.TryGetValue("w", out var w) || !w.IsInt32 || w.AsInt32 <= 0 || !doc.TryGetValue("h", out var h) || !h.IsInt32 || h.AsInt32 <= 0)
            return Unavailable<ProviderImage>();
        return Result<ProviderImage>.Success(new(url, Url(doc, "thumbUrl"), w.AsInt32, h.AsInt32));
    }
    private static Result<T> Unavailable<T>() => Result<T>.Failure(NexoCodes.ServerDependencyUnavailable, "No se pudo comprobar el contenido.");
}

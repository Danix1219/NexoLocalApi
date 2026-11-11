using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Infrastructure.Persistence;

namespace Nexo.Identity.Infrastructure.Safety;

public sealed class ContentSafetyOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>No aprueba nombres si la configuracion, respuesta o dependencia no es verificable.</summary>
public sealed class AccountProfileSafety(HttpClient client, IOptions<ContentSafetyOptions> options, IdentityCollections collections) : IAccountProfileSafety
{
    private static readonly string[] Categories = ["Hate", "Sexual", "Violence", "SelfHarm"];

    public async Task<Result<bool>> CheckNameAsync(string name, CancellationToken ct)
    {
        var settings = options.Value;
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != "https" ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || string.IsNullOrWhiteSpace(settings.ApiKey))
            return Unavailable<bool>();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(endpoint, "/contentsafety/text:analyze?api-version=2024-09-01"));
            request.Headers.Add("Ocp-Apim-Subscription-Key", settings.ApiKey);
            request.Content = JsonContent.Create(new { text = name, categories = Categories, outputType = "FourSeverityLevels" });
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return Unavailable<bool>();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("categoriesAnalysis", out var analysis) || analysis.ValueKind != JsonValueKind.Array)
                return Unavailable<bool>();
            var found = new HashSet<string>(StringComparer.Ordinal);
            var rejected = false;
            foreach (var item in analysis.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("category", out var category) || category.ValueKind != JsonValueKind.String ||
                    !Categories.Contains(category.GetString(), StringComparer.Ordinal) || !found.Add(category.GetString()!) ||
                    !item.TryGetProperty("severity", out var severity) || severity.ValueKind != JsonValueKind.Number ||
                    !severity.TryGetInt32(out var value) || value is not (0 or 2 or 4 or 6))
                    return Unavailable<bool>();
                rejected |= value >= 2;
            }
            if (found.Count != Categories.Length) return Unavailable<bool>();
            return rejected ? Result<bool>.Failure(NexoCodes.ContentInappropriate, "El nombre no puede publicarse.") : Result<bool>.Success(true);
        }
        catch (HttpRequestException) { return Unavailable<bool>(); }
        catch (JsonException) { return Unavailable<bool>(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Unavailable<bool>(); }
    }

    public async Task<Result<string>> ResolvePhotoAsync(Guid accountId, Guid uploadId, CancellationToken ct)
    {
        // La propiedad, finalidad y aprobacion van en la misma consulta. No se revela si es ajena.
        var f = Builders<BsonDocument>.Filter;
        var photo = await collections.Uploads.Find(f.Eq("_id", uploadId) & f.Eq("ownerId", accountId) &
                f.Eq("status", "approved") & f.Eq("purpose", "avatar"))
            .Project(Builders<BsonDocument>.Projection.Include("url").Exclude("_id")).FirstOrDefaultAsync(ct);
        if (photo is null) return Result<string>.Failure(NexoCodes.UploadNotFound, "La foto no esta disponible.");
        var url = photo.GetValue("url", BsonNull.Value);
        if (!url.IsString || url.AsString.Length > 200 || !Uri.TryCreate(url.AsString, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            return Unavailable<string>();
        return Result<string>.Success(url.AsString);
    }

    private static Result<T> Unavailable<T>() => Result<T>.Failure(NexoCodes.ServerDependencyUnavailable, "No se pudo comprobar el contenido.");
}

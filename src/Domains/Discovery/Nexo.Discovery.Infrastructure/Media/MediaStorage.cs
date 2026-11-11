using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using Nexo.Discovery.Infrastructure.Providers;

namespace Nexo.Discovery.Infrastructure.Media;
public sealed class MediaOptions
{
    public string ConnectionString { get; set; } = "";
    public string QueueConnectionString { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
    public bool DisableWorker { get; set; }
    public bool DisableMaintenance { get; set; }
}
public interface IMediaStorage
{
    Task EnsureUploadInfrastructureAsync(CancellationToken ct);
    string Ticket(Guid id);
    Task<bool> ExistsAsync(Guid id,CancellationToken ct);
    Task<byte[]> ReadIncomingAsync(Guid id,int size,CancellationToken ct);
    Task WriteAsync(string path,byte[] data,CancellationToken ct);
    Task<byte[]> ReadAsync(string path,CancellationToken ct);
    Task RemoveAsync(Guid id,string? path,CancellationToken ct);
    Task EnqueueAsync(Guid id,CancellationToken ct);
}
public sealed class AzureMediaStorage(IOptions<MediaOptions> options) : IMediaStorage
{
    private BlobServiceClient Blobs => new(options.Value.ConnectionString);
    public QueueClient Queue => new(string.IsNullOrWhiteSpace(options.Value.QueueConnectionString) ? options.Value.ConnectionString : options.Value.QueueConnectionString,"image-processing",new QueueClientOptions { MessageEncoding=QueueMessageEncoding.Base64 });
    public async Task EnsureUploadInfrastructureAsync(CancellationToken ct)
    {
        await Blobs.GetBlobContainerClient("incoming").CreateIfNotExistsAsync(cancellationToken:ct);
        await Blobs.GetBlobContainerClient("public").CreateIfNotExistsAsync(cancellationToken:ct);
        await Queue.CreateIfNotExistsAsync(cancellationToken:ct);
    }
    private BlobClient Incoming(Guid id)=>Blobs.GetBlobContainerClient("incoming").GetBlobClient(id.ToString("D"));
    public string Ticket(Guid id) => Incoming(id).GenerateSasUri(new BlobSasBuilder
    {
        BlobContainerName="incoming",BlobName=id.ToString("D"),Resource="b",ExpiresOn=DateTimeOffset.UtcNow.AddMinutes(15),
        Protocol=options.Value.ConnectionString.Contains("UseDevelopmentStorage=true",StringComparison.OrdinalIgnoreCase)?SasProtocol.None:SasProtocol.Https,
    }.CreateOnly()).ToString();
    public async Task<bool> ExistsAsync(Guid id,CancellationToken ct)=>(await Incoming(id).ExistsAsync(ct)).Value;
    public async Task<byte[]> ReadIncomingAsync(Guid id,int size,CancellationToken ct)
    {
        var blob=Incoming(id); var properties=(await blob.GetPropertiesAsync(cancellationToken:ct)).Value;
        if(properties.ContentLength!=size || properties.ContentLength>5242880) throw new InvalidDataException("upload.too_large");
        var result=await blob.DownloadContentAsync(new BlobDownloadOptions { Conditions=new BlobRequestConditions { IfMatch=properties.ETag } },ct);
        var bytes=result.Value.Content.ToArray();
        if(bytes.Length!=size) throw new InvalidDataException("upload.too_large");
        return bytes;
    }
    private BlobClient Public(string path)=>Blobs.GetBlobContainerClient("public").GetBlobClient(path);
    public async Task WriteAsync(string path,byte[] data,CancellationToken ct)=>await Public(path).UploadAsync(BinaryData.FromBytes(data),new BlobUploadOptions { HttpHeaders=new BlobHttpHeaders { ContentType="image/webp",CacheControl="no-store" } },ct);
    public async Task<byte[]> ReadAsync(string path,CancellationToken ct)=>(await Public(path).DownloadContentAsync(ct)).Value.Content.ToArray();
    public async Task RemoveAsync(Guid id,string? path,CancellationToken ct)
    {
        await Incoming(id).DeleteIfExistsAsync(cancellationToken:ct);
        if(path!=null) { await Public(path).DeleteIfExistsAsync(cancellationToken:ct); await Public(path+".thumb").DeleteIfExistsAsync(cancellationToken:ct); }
    }
    public async Task EnqueueAsync(Guid id,CancellationToken ct)=>await Queue.SendMessageAsync(id.ToString("D"),cancellationToken:ct);
}
internal static class MediaSas
{
    public static BlobSasBuilder CreateOnly(this BlobSasBuilder sas) { sas.SetPermissions(BlobSasPermissions.Create); return sas; }
}
public interface IImageModeration { Task<bool> ApproveAsync(byte[] normalizedPng,CancellationToken ct); }
public sealed class ImageModeration(HttpClient client,IOptions<ProviderSafetyOptions> options) : IImageModeration
{
    public async Task<bool> ApproveAsync(byte[] png,CancellationToken ct)
    {
        var config=options.Value;
        if(!Uri.TryCreate(config.Endpoint,UriKind.Absolute,out var endpoint) || endpoint.Scheme!="https" || endpoint.UserInfo.Length!=0 || string.IsNullOrWhiteSpace(config.ApiKey)) throw new HttpRequestException("Content Safety no configurado");
        using var request=new HttpRequestMessage(HttpMethod.Post,new Uri(endpoint,"/contentsafety/image:analyze?api-version=2024-09-01"));
        request.Headers.Add("Ocp-Apim-Subscription-Key",config.ApiKey);
        request.Content=JsonContent.Create(new { image=new { content=Convert.ToBase64String(png) }, categories=new[]{"Hate","Sexual","Violence","SelfHarm"},outputType="FourSeverityLevels" });
        using var response=await client.SendAsync(request,ct); response.EnsureSuccessStatusCode();
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if(!doc.RootElement.TryGetProperty("categoriesAnalysis",out var analysis) || analysis.ValueKind!=JsonValueKind.Array) throw new HttpRequestException("Respuesta incompleta");
        var found=new HashSet<string>(); var rejected=false;
        foreach(var row in analysis.EnumerateArray())
        {
            if(!row.TryGetProperty("category",out var c) || c.ValueKind!=JsonValueKind.String || !new[]{"Hate","Sexual","Violence","SelfHarm"}.Contains(c.GetString()) || !found.Add(c.GetString()!) ||
                !row.TryGetProperty("severity",out var s) || !s.TryGetInt32(out var n) || n is not (0 or 2 or 4 or 6)) throw new HttpRequestException("Respuesta incompleta");
            // Images: allow low severity; block medium/high. Text policy is independent.
            rejected|=n>=4;
        }
        if(found.Count!=4) throw new HttpRequestException("Respuesta incompleta");
        return !rejected;
    }
}

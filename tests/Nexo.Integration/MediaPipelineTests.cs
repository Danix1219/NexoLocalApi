using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Discovery.Application.Media;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Infrastructure.Media;
using Nexo.Discovery.Infrastructure.Providers;
using SkiaSharp;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class MediaPipelineTests(InfraFixture infra)
{
    public sealed class Blobs : IMediaStorage
    {
        public Dictionary<Guid, byte[]> Incoming { get; } = [];
        public Dictionary<string, byte[]> Published { get; } = [];
        public int Queued; public bool FailAfterOriginalRemoval;
        public Task EnsureUploadInfrastructureAsync(CancellationToken ct) => Task.CompletedTask;
        public string Ticket(Guid id) => "https://test.blob.core.windows.net/incoming/" + id + "?sp=c";
        public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(Incoming.ContainsKey(id));
        public Task<byte[]> ReadIncomingAsync(Guid id, int size, CancellationToken ct)
        { var b = Incoming[id]; if (b.Length != size) throw new InvalidDataException("upload.too_large"); return Task.FromResult(b); }
        public Task WriteAsync(string path, byte[] data, CancellationToken ct) { Published[path] = data; return Task.CompletedTask; }
        public Task<byte[]> ReadAsync(string path, CancellationToken ct) => Task.FromResult(Published[path]);
        public Task RemoveAsync(Guid id, string? path, CancellationToken ct) { Incoming.Remove(id); if (FailAfterOriginalRemoval && path == null) { FailAfterOriginalRemoval = false; throw new HttpRequestException(); } if (path != null) { Published.Remove(path); Published.Remove(path + ".thumb"); } return Task.CompletedTask; }
        public Task EnqueueAsync(Guid id, CancellationToken ct) { Queued++; return Task.CompletedTask; }
    }
    public sealed class Moderation : IImageModeration
    {
        public bool Approved = true; public bool Down; public int Calls;
        public Task<bool> ApproveAsync(byte[] bytes, CancellationToken ct) { Calls++; if (Down) throw new HttpRequestException(); return Task.FromResult(Approved); }
    }
    public sealed class Accounts : IProviderAccounts
    {
        public bool Active = true; public bool Deleted; public string? PhotoUrl;
        public Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct) => Task.FromResult<ProviderAccountState?>(new(Active, true, true, PhotoUrl, Deleted));
        public Task<bool> EnableProviderAsync(Guid id, CancellationToken ct) => Task.FromResult(true);
    }
    public sealed class Env : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    public static byte[] Image(int w = 80, int h = 100)
    { using var b = new SKBitmap(w, h); using (var c = new SKCanvas(b)) c.Clear(SKColors.Beige); using var i = SKImage.FromBitmap(b); using var d = i.Encode(SKEncodedImageFormat.Png, 100); return d.ToArray(); }
    private (MongoMediaService Service, IMongoDatabase Db, Blobs Storage, Moderation Safety, Accounts Accounts) Setup()
    {
        var db = new MongoClient(infra.MongoConnectionString).GetDatabase("media_" + Guid.NewGuid().ToString("N"));
        var storage = new Blobs(); var safety = new Moderation(); var accounts = new Accounts();
        return (new(db, storage, safety, Options.Create(new MediaOptions()), new Env(), accounts, new MongoProviderStore(db)), db, storage, safety, accounts);
    }
    [Fact]
    public async Task Intencion_propiedad_confirmacion_idempotente_y_aprobacion_real_del_codec()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var bytes = Image();
        var ticket = await s.Service.CreateAsync(owner, new("avatar", "image/png", bytes.Length), default); Assert.True(ticket.IsSuccess);
        var id = ticket.Value!.UploadId; s.Storage.Incoming[id] = bytes;
        Assert.Equal("upload.not_found", (await s.Service.CompleteAsync(Guid.NewGuid(), id, default)).Error!.Code);
        Assert.Equal("processing", (await s.Service.CompleteAsync(owner, id, default)).Value!.Status);
        await s.Service.CompleteAsync(owner, id, default); Assert.Equal(1, s.Storage.Queued);
        Assert.Null(await s.Service.FileAsync(id, false, default));
        await Task.WhenAll(s.Service.ProcessAsync(id, default), s.Service.ProcessAsync(id, default));
        Assert.Equal(1, s.Safety.Calls); Assert.False(s.Storage.Incoming.ContainsKey(id));
        var state = (await s.Service.StateAsync(owner, id, default)).Value!; Assert.Equal("approved", state.Status); Assert.Equal(80, state.Width);
        Assert.NotNull(await s.Service.AvatarAsync(owner, id, default)); Assert.Null(await s.Service.AvatarAsync(Guid.NewGuid(), id, default));
        using var image = SKBitmap.Decode((await s.Service.FileAsync(id, true, default))!.Bytes); Assert.Equal(400, image.Width); Assert.Equal(400, image.Height);
        Assert.Equal("approved", (await s.Service.CompleteAsync(owner, id, default)).Value!.Status);
        await s.Service.DeleteAsync(owner, id, default); Assert.Null(await s.Service.FileAsync(id, false, default)); Assert.Empty(s.Storage.Published);
    }
    [Fact]
    public async Task Rechazo_no_publica_y_fallo_externo_reintenta_sin_aprobar()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var bytes = Image();
        var id = (await s.Service.CreateAsync(owner, new("request", "image/png", bytes.Length), default)).Value!.UploadId; s.Storage.Incoming[id] = bytes;
        await s.Service.CompleteAsync(owner, id, default); s.Safety.Down = true; await s.Service.ProcessAsync(id, default);
        Assert.Equal("processing", (await s.Service.StateAsync(owner, id, default)).Value!.Status); Assert.Empty(s.Storage.Published);
        s.Safety.Down = false; s.Safety.Approved = false;
        await s.Db.GetCollection<BsonDocument>("uploads").UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, new BsonDocument("$set", new BsonDocument("nextAttempt", DateTime.UtcNow.AddMinutes(-1))));
        await s.Service.ProcessAsync(id, default);
        Assert.Equal("upload.rejected_by_moderation", (await s.Service.StateAsync(owner, id, default)).Error!.Code); Assert.Empty(s.Storage.Published); Assert.Empty(s.Storage.Incoming);
    }
    [Fact]
    public async Task Caducidad_cuota_limites_y_portada_solo_propia_y_aprobada()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var bytes = Image();
        Assert.Equal("provider.profile_required", (await s.Service.CreateAsync(owner, new("work", "image/png", bytes.Length), default)).Error!.Code);
        var id = (await s.Service.CreateAsync(owner, new("cover", "image/png", bytes.Length), default)).Value!.UploadId;
        Assert.Equal("upload.blob_missing", (await s.Service.CompleteAsync(owner, id, default)).Error!.Code);
        Assert.True((await s.Service.SetCoverAsync(owner, id, default)).IsFailure);
        s.Storage.Incoming[id] = bytes; await s.Service.CompleteAsync(owner, id, default); await s.Service.ProcessAsync(id, default);
        Assert.True((await s.Service.SetCoverAsync(Guid.NewGuid(), id, default)).IsFailure);
        Assert.True((await s.Service.SetCoverAsync(owner, id, default)).IsSuccess); Assert.NotNull(await s.Service.CoverAsync(owner, default));
        for (var i = 1; i < 30; i++) Assert.True((await s.Service.CreateAsync(owner, new("avatar", "image/png", bytes.Length), default)).IsSuccess);
        Assert.Equal("rate.limit_exceeded", (await s.Service.CreateAsync(owner, new("avatar", "image/png", bytes.Length), default)).Error!.Code);
        await s.Db.GetCollection<BsonDocument>("uploads").UpdateManyAsync(new BsonDocument("status", "pending"), new BsonDocument("$set", new BsonDocument("expiresAt", DateTime.UtcNow.AddHours(-1))));
        await s.Service.RecoverAsync(default);
        await s.Service.RecoverAsync(default);
        Assert.Equal(0, await s.Db.GetCollection<BsonDocument>("uploads").CountDocumentsAsync(new BsonDocument("status", "pending")));
    }
    [Fact]
    public void Bytes_tipo_dimensiones_y_salida_webp_sin_metadatos_originales()
    {
        var image = ImageCodec.Encode(Image(2000, 1000), "image/png"); Assert.Equal(1600, image.Width); Assert.Equal(800, image.Height);
        using var data = SKData.CreateCopy(image.Full); using var codec = SKCodec.Create(data); Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Throws<InvalidDataException>(() => ImageCodec.Encode(Image(), "image/jpeg"));
        Assert.Throws<InvalidDataException>(() => ImageCodec.Encode([1, 2, 3], "image/png"));
        Assert.Throws<InvalidDataException>(() => ImageCodec.Encode(Image(4097, 1), "image/png"));
        Assert.Equal("upload.too_large", MediaRules.Validate(new("avatar", "image/png", 5242881)));
        Assert.Equal("upload.content_type_invalid", MediaRules.Validate(new("avatar", "text/html", 100)));
        Assert.Equal("validation_failed", MediaRules.Validate(new("chat", "image/png", 100)));
    }
    [Fact]
    public async Task Recupera_aprobacion_tras_borrar_original_y_respeta_referencias_al_limpiar()
    {
        var s = Setup(); var owner = Guid.NewGuid(); var bytes = Image();
        var id = (await s.Service.CreateAsync(owner, new("avatar", "image/png", bytes.Length), default)).Value!.UploadId;
        s.Storage.Incoming[id] = bytes; await s.Service.CompleteAsync(owner, id, default);
        s.Storage.FailAfterOriginalRemoval = true; await s.Service.ProcessAsync(id, default);
        Assert.Empty(s.Storage.Incoming); Assert.Equal("processing", (await s.Service.StateAsync(owner, id, default)).Value!.Status);
        var uploads = s.Db.GetCollection<BsonDocument>("uploads");
        await uploads.UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, new BsonDocument("$set", new BsonDocument("nextAttempt", DateTime.UtcNow.AddMinutes(-1))));
        await s.Service.ProcessAsync(id, default); Assert.Equal(1, s.Safety.Calls);
        var state = (await s.Service.StateAsync(owner, id, default)).Value!; Assert.Equal("approved", state.Status);
        s.Accounts.PhotoUrl = state.Url;
        async Task Due() => await uploads.UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, new BsonDocument("$set", new BsonDocument("nextAudit", DateTime.UtcNow.AddMinutes(-1))));
        await Due(); await s.Service.AuditAsync(default); Assert.NotNull(await s.Service.FileAsync(id, false, default));
        s.Accounts.Active = false; s.Accounts.PhotoUrl = null;
        await Due(); await s.Service.AuditAsync(default); Assert.Equal("approved", (await s.Service.StateAsync(owner, id, default)).Value!.Status);
        s.Accounts.Deleted = true; await Due(); await s.Service.AuditAsync(default);
        Assert.Empty(s.Storage.Published); Assert.Null(await s.Service.FileAsync(id, false, default));
    }
    private sealed class ImageResponse(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Contains("/contentsafety/image:analyze", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(4, false)]
    [InlineData(6, false)]
    public async Task Moderacion_imagen_permite_bajo_bloquea_medio_alto_y_falla_cerrado(int level, bool expected)
    {
        foreach (var target in new[] { "Hate", "Sexual", "Violence", "SelfHarm" })
        {
            var body = System.Text.Json.JsonSerializer.Serialize(new { categoriesAnalysis = new[] { "Hate", "Sexual", "Violence", "SelfHarm" }.Select(c => new { category = c, severity = c == target ? level : 0 }) });
            using var client = new HttpClient(new ImageResponse(body));
            var adapter = new ImageModeration(client, Options.Create(new ProviderSafetyOptions { Endpoint = "https://test.example", ApiKey = "prueba-sin-valor" }));
            Assert.Equal(expected, await adapter.ApproveAsync(Image(), default));
        }
        using var invalid = new HttpClient(new ImageResponse("{\"categoriesAnalysis\":[]}"));
        var bad = new ImageModeration(invalid, Options.Create(new ProviderSafetyOptions { Endpoint = "https://test.example", ApiKey = "prueba-sin-valor" }));
        await Assert.ThrowsAsync<HttpRequestException>(() => bad.ApproveAsync(Image(), default));
    }
    [Fact]
    public void Reorienta_EXIF_y_no_copia_segmentos_de_metadatos_a_WebP()
    {
        using var bitmap = new SKBitmap(80, 100); using (var c = new SKCanvas(bitmap)) c.Clear(SKColors.Beige);
        using var image = SKImage.FromBitmap(bitmap); using var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 80);
        // APP1: EXIF/TIFF orientation 6 (90 clockwise), stripped by decode/re-encode.
        byte[] payload = [69, 120, 105, 102, 0, 0, 73, 73, 42, 0, 8, 0, 0, 0, 1, 0, 18, 1, 3, 0, 1, 0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0];
        var source = jpeg.ToArray(); var bytes = new byte[source.Length + payload.Length + 4];
        Array.Copy(source, 0, bytes, 0, 2); bytes[2] = 255; bytes[3] = 225; bytes[4] = 0; bytes[5] = (byte)(payload.Length + 2);
        Array.Copy(payload, 0, bytes, 6, payload.Length); Array.Copy(source, 2, bytes, payload.Length + 6, source.Length - 2);
        var encoded = ImageCodec.Encode(bytes, "image/jpeg"); Assert.Equal(100, encoded.Width); Assert.Equal(80, encoded.Height);
        Assert.False(System.Text.Encoding.Latin1.GetString(encoded.Full).Contains("Exif", StringComparison.Ordinal));
    }
}

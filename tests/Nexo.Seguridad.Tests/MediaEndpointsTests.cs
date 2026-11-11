using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nexo.Discovery.Application.Media;
using Nexo.Discovery.Infrastructure.Media;
using SkiaSharp;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Infrastructure.Providers;
using Nexo.Discovery.Domain;

namespace Nexo.Seguridad.Tests;
[Collection("seguridad")]
public sealed class MediaEndpointsTests(InfraDeSeguridad infra)
{
    private sealed class Storage : IMediaStorage
    {
        public Dictionary<Guid,byte[]> Incoming=[];public Dictionary<string,byte[]> Files=[];
        public Task EnsureUploadInfrastructureAsync(CancellationToken ct)=>Task.CompletedTask;
        public string Ticket(Guid id)=>"https://test.blob.core.windows.net/incoming/"+id+"?sp=c";
        public Task<bool> ExistsAsync(Guid id,CancellationToken ct)=>Task.FromResult(Incoming.ContainsKey(id));
        public Task<byte[]> ReadIncomingAsync(Guid id,int size,CancellationToken ct)=>Task.FromResult(Incoming[id]);
        public Task WriteAsync(string path,byte[] bytes,CancellationToken ct){Files[path]=bytes;return Task.CompletedTask;}
        public Task<byte[]> ReadAsync(string path,CancellationToken ct)=>Task.FromResult(Files[path]);
        public Task RemoveAsync(Guid id,string? path,CancellationToken ct){Incoming.Remove(id);if(path!=null){Files.Remove(path);Files.Remove(path+".thumb");}return Task.CompletedTask;}
        public Task EnqueueAsync(Guid id,CancellationToken ct)=>Task.CompletedTask;
    }
    private sealed class Safety : IImageModeration {public Task<bool> ApproveAsync(byte[] image,CancellationToken ct)=>Task.FromResult(true);}
    private static async Task<JsonElement> Json(HttpResponseMessage response)=>JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    [Fact]
    public async Task JWT_propiedad_estados_avatar_identidad_portada_y_lectura_solo_aprobada()
    {
        var storage=new Storage();
        using var host=new HostDeSeguridad(infra.CadenaMongo,infra.CadenaRedis).WithWebHostBuilder(b=>b.ConfigureServices(s=>
        {
            s.RemoveAll<IMediaStorage>();s.AddSingleton<IMediaStorage>(storage);
            s.RemoveAll<IImageModeration>();s.AddSingleton<IImageModeration,Safety>();
            s.RemoveAll<IProviderSafety>();s.AddScoped<IProviderSafety>(sp=>new TextSafeImagesReal(new ProviderSafety(new HttpClient(),Options.Create(new ProviderSafetyOptions()),sp.GetRequiredKeyedService<IMongoDatabase>(NexoDatabaseKeys.Discovery))));
        }));
        using var client=host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync("/v1/discovery/uploads",new {purpose="avatar",contentType="image/png",size=100})).StatusCode);
        var account=await Cuenta.CrearAsync(host,client,$"media-{Guid.NewGuid():N}@example.test");client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",account.AccessToken);
        using var bitmap=new SKBitmap(80,80);using(var canvas=new SKCanvas(bitmap))canvas.Clear(SKColors.Beige);
        using var image=SKImage.FromBitmap(bitmap);using var encoded=image.Encode(SKEncodedImageFormat.Png,100);var bytes=encoded.ToArray();
        var ticket=await client.PostAsJsonAsync("/v1/discovery/uploads",new {purpose="avatar",contentType="image/png",size=bytes.Length});Assert.Equal(HttpStatusCode.Created,ticket.StatusCode);
        var id=(await Json(ticket)).GetProperty("uploadId").GetGuid();storage.Incoming[id]=bytes;
        using var stranger=host.CreateClient();var other=await Cuenta.CrearAsync(host,stranger,$"media-other-{Guid.NewGuid():N}@example.test");stranger.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",other.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound,(await stranger.PostAsync($"/v1/discovery/uploads/{id}/complete",null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/v1/discovery/media/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted,(await client.PostAsync($"/v1/discovery/uploads/{id}/complete",null)).StatusCode);
        using(var scope=host.Services.CreateScope())await scope.ServiceProvider.GetRequiredService<MongoMediaService>().ProcessAsync(id,default);
        Assert.Equal("approved",(await Json(await client.GetAsync($"/v1/discovery/uploads/{id}"))).GetProperty("status").GetString());
        var patch=await client.PatchAsJsonAsync("/v1/identity/me",new {photoUploadId=id});Assert.Equal(HttpStatusCode.OK,patch.StatusCode);
        var mine=await Json(await client.GetAsync("/v1/identity/me"));Assert.Contains(id.ToString(),mine.GetProperty("photoUrl").GetString());
        var read=await client.GetAsync($"/v1/discovery/media/{id}");Assert.Equal(HttpStatusCode.OK,read.StatusCode);Assert.Equal("image/webp",read.Content.Headers.ContentType!.MediaType);Assert.True(read.Headers.CacheControl!.Private);Assert.Equal(TimeSpan.FromMinutes(5),read.Headers.CacheControl.MaxAge);
        async Task<Guid> Upload(string purpose)
        {
            var response=await client.PostAsJsonAsync("/v1/discovery/uploads",new {purpose,contentType="image/png",size=bytes.Length});
            Assert.Equal(HttpStatusCode.Created,response.StatusCode);
            var upload=(await Json(response)).GetProperty("uploadId").GetGuid();storage.Incoming[upload]=bytes;
            Assert.Equal(HttpStatusCode.Accepted,(await client.PostAsync($"/v1/discovery/uploads/{upload}/complete",null)).StatusCode);
            using var scope=host.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<MongoMediaService>().ProcessAsync(upload,default);return upload;
        }
        var cover=await Upload("cover");
        Assert.Equal(HttpStatusCode.NotFound,(await stranger.PutAsJsonAsync("/v1/discovery/me/cover",new {uploadId=cover})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync("/v1/discovery/me/cover",new {uploadId=cover})).StatusCode);
        Assert.Contains(cover.ToString(),(await Json(await client.GetAsync("/v1/discovery/me/cover"))).GetProperty("url").GetString());
        var requestImage=await Upload("request");
        using var publication=new HttpRequestMessage(HttpMethod.Post,"/v1/discovery/requests") {Content=JsonContent.Create(new {
            category="plomeria",title="Reparar tuberia",description="Necesito reparar una fuga en la tuberia del lavabo.",urgency="week",contact="chat",location=new {lat=20.05,lng=-99.34},imageUploadId=requestImage })};
        publication.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());
        var published=await client.SendAsync(publication);Assert.Equal(HttpStatusCode.Created,published.StatusCode);
        var owner=mine.GetProperty("accountId").GetGuid();
        using(var scope=host.Services.CreateScope())
        {
            var profile=new ProviderProfile {ProviderId=Guid.NewGuid(),Name="Prestador de prueba",Categories=["plomeria"],Location=new(20.05,-99.34),PublicZone=new("Zona aproximada",new(20.056,-99.346)),CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow};
            Assert.True(await scope.ServiceProvider.GetRequiredService<IProviderStore>().InsertAsync(new(owner,profile,0,Guid.NewGuid().ToString(),"hash",true),default));
        }
        var workImage=await Upload("work");
        using var work=new HttpRequestMessage(HttpMethod.Post,"/v1/discovery/works") {Content=JsonContent.Create(new {
            category="plomeria",title="Trabajo de prueba",text="Reparamos las tuberias y comprobamos el funcionamiento.",problem="Fuga en el calentador",did="Cambio de valvula",time="2 horas",tag="Terminado",location=new {lat=20.05,lng=-99.34},imageUploadIds=new[]{workImage} })};
        work.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());
        var created=await client.SendAsync(work);Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        Assert.Contains(workImage.ToString(),await created.Content.ReadAsStringAsync());
        Assert.Contains(requestImage.ToString(),await (await client.GetAsync("/v1/discovery/requests/mine")).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound,(await stranger.DeleteAsync($"/v1/discovery/uploads/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/v1/discovery/uploads/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/v1/discovery/media/{id}")).StatusCode);
    }
    private sealed class TextSafeImagesReal(ProviderSafety images) : IProviderSafety
    {
        public Task<Result<bool>> ModerateAsync(string text,CancellationToken ct)=>Task.FromResult(Result<bool>.Success(true));
        public Task<Result<ProviderImage>> ImageAsync(Guid owner,Guid upload,string purpose,CancellationToken ct)=>images.ImageAsync(owner,upload,purpose,ct);
    }
    [Theory]
    [InlineData("text/html",100,400)]
    [InlineData("image/png",5242881,413)]
    public async Task Validacion_antes_de_emitir_SAS(string type,int size,int expected)
    {
        using var host=new HostDeSeguridad(infra.CadenaMongo,infra.CadenaRedis);using var client=host.CreateClient();
        var account=await Cuenta.CrearAsync(host,client,$"media-invalid-{Guid.NewGuid():N}@example.test");client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",account.AccessToken);
        Assert.Equal(expected,(int)(await client.PostAsJsonAsync("/v1/discovery/uploads",new {purpose="avatar",contentType=type,size})).StatusCode);
    }
}

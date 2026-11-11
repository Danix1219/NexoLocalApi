using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Discovery.Infrastructure.Providers;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class ProviderSafetyTests(InfraFixture infra)
{
    private sealed class ResponseHandler(string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Assert.EndsWith("/contentsafety/text:analyze?api-version=2024-09-01", request.RequestUri!.AbsoluteUri);
            Assert.Contains("FourSeverityLevels", await request.Content!.ReadAsStringAsync(ct));
            return new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
    [Theory]
    [InlineData("{\"categoriesAnalysis\":[{\"category\":\"Hate\",\"severity\":0},{\"category\":\"Sexual\",\"severity\":0},{\"category\":\"Violence\",\"severity\":0},{\"category\":\"SelfHarm\",\"severity\":0}]}", null)]
    [InlineData("{\"categoriesAnalysis\":[{\"category\":\"Hate\",\"severity\":2},{\"category\":\"Sexual\",\"severity\":0},{\"category\":\"Violence\",\"severity\":0},{\"category\":\"SelfHarm\",\"severity\":0}]}", NexoCodes.ContentInappropriate)]
    [InlineData("{\"categoriesAnalysis\":[]}", NexoCodes.ServerDependencyUnavailable)]
    [InlineData("{\"categoriesAnalysis\":[{\"category\":\"Hate\",\"severity\":\"0\"}]}", NexoCodes.ServerDependencyUnavailable)]
    [InlineData("[]", NexoCodes.ServerDependencyUnavailable)]
    public async Task Adaptador_de_moderacion_comprueba_todas_las_categorias(string body, string? expected)
    {
        using var handler = new ResponseHandler(body); using var client = new HttpClient(handler);
        var safety = new ProviderSafety(client, Options.Create(new ProviderSafetyOptions { Endpoint = "https://moderation.example.test", ApiKey = "sin-valor-de-prueba" }),
            new MongoClient(infra.MongoConnectionString).GetDatabase("safety"));
        var result = await safety.ModerateAsync("Contenido de prueba", default);
        Assert.Equal(expected, result.Error?.Code);
        Assert.Equal(1, handler.Calls);
        var unconfigured = new ProviderSafety(client, Options.Create(new ProviderSafetyOptions()), new MongoClient(infra.MongoConnectionString).GetDatabase("safety"));
        Assert.Equal(NexoCodes.ServerDependencyUnavailable, (await unconfigured.ModerateAsync("Nombre", default)).Error!.Code);
        Assert.Equal(1, handler.Calls);
    }
    [Fact]
    public async Task Subida_exige_propietario_aprobacion_finalidad_y_URL_segura()
    {
        var db = new MongoClient(infra.MongoConnectionString).GetDatabase("uploads_" + Guid.NewGuid().ToString("N"));
        var owner = Guid.NewGuid(); var upload = Guid.NewGuid();
        var collection = db.GetCollection<BsonDocument>("uploads");
        await collection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = new BsonBinaryData(upload, GuidRepresentation.Standard), ["ownerId"] = new BsonBinaryData(owner, GuidRepresentation.Standard),
            ["purpose"] = "avatar", ["status"] = "approved", ["url"] = "https://cdn.example.test/image.webp", ["w"] = 400, ["h"] = 400
        });
        using var client = new HttpClient();
        var safety = new ProviderSafety(client, Options.Create(new ProviderSafetyOptions()), db);
        Assert.True((await safety.ImageAsync(owner, upload, "avatar", default)).IsSuccess);
        Assert.Equal(NexoCodes.UploadNotFound, (await safety.ImageAsync(Guid.NewGuid(), upload, "avatar", default)).Error!.Code);
        Assert.Equal(NexoCodes.UploadNotFound, (await safety.ImageAsync(owner, upload, "cover", default)).Error!.Code);
        await collection.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty, new BsonDocument("$set", new BsonDocument("status", "rejected")));
        Assert.Equal(NexoCodes.UploadNotFound, (await safety.ImageAsync(owner, upload, "avatar", default)).Error!.Code);
    }
}

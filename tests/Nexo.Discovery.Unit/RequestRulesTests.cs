using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Directory;
using Nexo.Discovery.Application.Requests;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Unit;

public sealed class RequestRulesTests
{
    private static PublishRequest Body => new("plomeria", "Fuga en el baño", "Necesito reparar una fuga debajo del lavabo.", "today", "chat", new(20.05, -99.34));
    [Theory]
    [InlineData(4, 10, false)]
    [InlineData(5, 10, true)]
    [InlineData(90, 600, true)]
    [InlineData(91, 10, false)]
    [InlineData(5, 9, false)]
    [InlineData(5, 601, false)]
    public void Limites_de_titulo_y_descripcion(int title, int description, bool valid) =>
        Assert.Equal(valid, RequestRules.Validate(Body with { Title = new('t', title), Description = new('d', description) }) == null);
    [Fact]
    public void Enums_categoria_coordenadas_y_foto_no_se_confian_al_cliente()
    {
        foreach (var bad in new[] { Body with { Category = "inventada" }, Body with { Contact = "sms" }, Body with { Urgency = "yesterday" },
            Body with { Location = new(double.NaN, -99) }, Body with { ImageUploadId = Guid.Empty } }) Assert.NotNull(RequestRules.Validate(bad));
        Assert.True(RequestRules.Valid(new())); Assert.False(RequestRules.Valid(new("invalid")));
        Assert.False(RequestRules.Valid(new("noLongerNeeded", Guid.NewGuid()))); Assert.True(RequestRules.Valid(new("solvedHere", Guid.NewGuid())));
    }
    [Fact]
    public void Cursor_ligado_a_cuenta_filtros_y_rango_de_urgencia()
    {
        var hash = RequestRules.Hash(new { owner = Guid.NewGuid(), status = "all" }); var id = Guid.CreateVersion7();
        var cursor = RequestRules.Cursor(id, hash, 2); Assert.InRange(cursor.Length, 1, 64);
        Assert.True(RequestRules.TryCursor(cursor, hash, out var decoded, out var rank)); Assert.Equal(id, decoded); Assert.Equal(2, rank);
        Assert.False(RequestRules.TryCursor(cursor, RequestRules.Hash(new { owner = Guid.NewGuid() }), out _));
        Assert.False(RequestRules.TryCursor("invalid", hash, out _)); Assert.False(RequestRules.TryCursor(new('x', 65), hash, out _));
        Assert.Equal("a ~1.5 km", RequestRules.DistanceText(1.47));
    }
    [Fact]
    public async Task Editar_solicitud_valida_contenido_propiedad_y_fotos()
    {
        var p = new Ports(); var id = Guid.NewGuid();
        p.Value = new ServiceRequest(id, "plomeria", "Fuga anterior", "Necesito reparar una fuga.", "week", "chat",
            new(20.05, -99.34), "Zona", null, "open", 0, 0, DateTime.UtcNow, DateTime.UtcNow);
        var images = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var valid = JsonSerializer.SerializeToElement(new { title = "Fuga renovada", description = "Necesito reparar dos fugas en casa.",
            urgency = "today", contact = "chat", imageUploadIds = images });
        var result = await p.Service().EditAsync(p.Owner, id, valid, default);
        Assert.True(result.IsSuccess); Assert.Equal(2, result.Value!.Photos.Length); Assert.Equal("Fuga renovada", result.Value.Title);
        Assert.Equal(1, p.Writes); Assert.Equal(1, p.Checks);
        Assert.Equal(NexoCodes.ContentImagesLimit, RequestRules.ValidateEdit(new("Fuga nueva", "Necesito ayuda en casa.", "week", "chat", Enumerable.Repeat(Guid.NewGuid(), 7).ToArray())));
        Assert.NotNull(RequestRules.ValidateEdit(new("Fuga nueva", "Necesito ayuda en casa.", "week", "chat", [images[0], images[0]])));
        p.Value = p.Value with { Status = "closed" };
        Assert.Equal(NexoCodes.RequestAlreadyClosed, (await p.Service().EditAsync(p.Owner, id, valid, default)).Error!.Code);
    }
    private sealed class Ports : IRequestStore, IProviderStore, IProviderAccounts, IProviderSafety, IDirectoryProfiles, IRequestClients, IRequestInterests
    {
        public ProviderAccountState Account = new(true, true, true);
        public readonly Guid Owner = Guid.NewGuid();
        public ServiceRequest? Value;
        public RequestReceipt? Receipt;
        public int Writes, Checks;
        public string? ModerationError;
        public string? CheckedText;
        public bool SummaryAvailable = true;
        public IReadOnlyList<StoredProvider> Providers = [];
        public RequestService Service() => new(this, this, this, this, this, this, this, TimeProvider.System);
        public Task<RequestReceipt?> ReceiptAsync(Guid owner, Guid key, CancellationToken ct) => Task.FromResult(Receipt);
        public Task<ServiceRequest?> OwnAsync(Guid owner, Guid id, CancellationToken ct) => Task.FromResult(Value);
        public Task<RequestCandidate?> VisibleAsync(Guid viewer, Guid id, ProviderPoint point, double radiusKm, CancellationToken ct) => Task.FromResult<RequestCandidate?>(null);
        public Task<Result<ServiceRequest>> CreateAsync(Guid owner, Guid key, string hash, ServiceRequest request, ProviderPoint exact, CancellationToken ct)
        { Writes++; Value = request; Receipt = new(hash, request.RequestId); return Task.FromResult(Result<ServiceRequest>.Success(request)); }
        public Task<Result<ServiceRequest>> UpdateAsync(Guid owner, Guid id, ServiceRequest request, CancellationToken ct)
        { Writes++; Value = request; return Task.FromResult(Result<ServiceRequest>.Success(request)); }
        public Task<ServiceRequest[]> MineAsync(Guid owner, string status, Guid? after, int limit, CancellationToken ct) => Task.FromResult(Value == null ? Array.Empty<ServiceRequest>() : new[] { Value });
        public Task<IReadOnlyList<RequestCandidate>> CandidatesAsync(Guid viewer, ProviderPoint point, double? radius, string[]? cats, string text, bool today, CancellationToken ct) => throw new NotSupportedException();
        public Task<Result<RequestClosure>> CloseAsync(Guid owner, Guid id, string outcome, Guid? declaredProvider, DateTime now, CancellationToken ct) => throw new NotSupportedException();
        public Task<StoredProvider?> OwnAsync(Guid id, CancellationToken ct) => Task.FromResult<StoredProvider?>(null);
        public Task<IReadOnlyList<StoredProvider>> PublicAsync(Guid[] ids, ProviderPoint? from, CancellationToken ct) => Task.FromResult<IReadOnlyList<StoredProvider>>([]);
        public Task<bool> InsertAsync(StoredProvider p, CancellationToken ct) => throw new NotSupportedException();
        public Task RecordViewAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> PatchAsync(StoredProvider p, ProviderProfile v, string s, string[] fields, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct) => Task.FromResult<ProviderAccountState?>(Account);
        public Task<bool> EnableProviderAsync(Guid id, CancellationToken ct) => throw new InvalidOperationException("Read-only state");
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct)
        { Checks++; CheckedText = text; return Task.FromResult(ModerationError == null ? Result<bool>.Success(true) : Result<bool>.Failure(ModerationError, "No disponible")); }
        public Task<Result<ProviderImage>> ImageAsync(Guid owner, Guid image, string purpose, CancellationToken ct) => Task.FromResult(Result<ProviderImage>.Success(new("https://cdn.test/v1/discovery/media/" + image, null, 1, 1)));
        public Task<IReadOnlyList<StoredProvider>> CandidatesAsync(ProviderPoint? point, string? cat, CancellationToken ct) => Task.FromResult(Providers);
        public Task<IReadOnlyDictionary<Guid, RequestClient>> ReadAsync(Guid[] ids, CancellationToken ct) => Task.FromResult<IReadOnlyDictionary<Guid, RequestClient>>(ids.ToDictionary(i => i, _ => new RequestClient(true, "P. T.", null)));
        public Task<RequestInterestResult> ReadAsync(Guid[] ids, Guid? provider, CancellationToken ct) => Task.FromResult(new RequestInterestResult(SummaryAvailable, new Dictionary<Guid, RequestInterest>()));
    }
    [Theory]
    [InlineData(NexoCodes.ContentInappropriate)]
    [InlineData(NexoCodes.ServerDependencyUnavailable)]
    public async Task Moderacion_rechazada_o_caida_no_persiste(string error)
    {
        var ports = new Ports { ModerationError = error };
        var result = await ports.Service().PublishAsync(ports.Owner, Guid.NewGuid().ToString(), JsonSerializer.SerializeToElement(Body), default);
        Assert.Equal(error, result.Error!.Code); Assert.Equal(0, ports.Writes);
    }
    [Theory]
    [InlineData(false, true, NexoCodes.AuthEmailNotVerified)]
    [InlineData(true, false, NexoCodes.PhoneInvalid)]
    public async Task ChatCall_exige_correo_y_movil_declarado(bool email, bool phone, string code)
    {
        var ports = new Ports { Account = new(true, email, phone) };
        var result = await ports.Service().PublishAsync(ports.Owner, Guid.NewGuid().ToString(), JsonSerializer.SerializeToElement(Body with { Contact = "chatCall" }), default);
        Assert.Equal(code, result.Error!.Code); Assert.Equal(0, ports.Writes); Assert.Equal(0, ports.Checks);
    }
    [Fact]
    public async Task Publicar_reintentar_y_no_fingir_interes_ni_imagenes()
    {
        var ports = new Ports();
        ports.Providers = [new(Guid.NewGuid(), new() { ProviderId = Guid.NewGuid(), RadiusKm = 5 }, 0, "", "", true, 1),
            new(Guid.NewGuid(), new() { RadiusKm = 2 }, 0, "", "", true, 8), new(ports.Owner, new() { RadiusKm = 5 }, 0, "", "", true, 1)];
        var service = ports.Service(); var key = Guid.NewGuid().ToString(); var input = JsonSerializer.SerializeToElement(Body);
        var result = await service.PublishAsync(ports.Owner, key, input, default);
        Assert.True(result.IsSuccess); Assert.Equal(1, result.Value!.ReachableProviders); Assert.Equal(0, result.Value.InterestCount);
        Assert.Contains(Body.Title, ports.CheckedText); Assert.Contains(Body.Description, ports.CheckedText);
        Assert.InRange(Body.Location.DistanceKm(result.Value.PublicPoint), .499, .801);
        Assert.Equal(result.Value, (await service.PublishAsync(ports.Owner, key, input, default)).Value); Assert.Equal(1, ports.Writes); Assert.Equal(1, ports.Checks);
        Assert.Equal(NexoCodes.RequestIdempotencyKeyReused, (await service.PublishAsync(ports.Owner, key, JsonSerializer.SerializeToElement(Body with { Urgency = "week" }), default)).Error!.Code);
        var other = new Ports(); var withImage = await other.Service().PublishAsync(other.Owner, Guid.NewGuid().ToString(),
            JsonSerializer.SerializeToElement(Body with { ImageUploadId = Guid.NewGuid() }), default);
        Assert.True(withImage.IsSuccess); Assert.Single(withImage.Value!.Photos); Assert.Equal(1, other.Writes);
    }
    [Fact]
    public async Task Resumen_caido_conserva_listado_y_contador_desnormalizado_sin_inventar_fotos()
    {
        var ports = new Ports { SummaryAvailable = false };
        ports.Value = new(Guid.CreateVersion7(), "plomeria", Body.Title, Body.Description, "today", "chat", new(20.06, -99.34), "Zona aproximada", null, "open", 3, 1, DateTime.UtcNow, DateTime.UtcNow);
        var result = await ports.Service().MineAsync(ports.Owner, "all", null, 20, default);
        var json = JsonSerializer.SerializeToElement(result.Value, ProviderService.Json);
        Assert.False(json.GetProperty("interestSummaryAvailable").GetBoolean()); Assert.Equal(3, json.GetProperty("items")[0].GetProperty("interestCount").GetInt32());
        Assert.Empty(json.GetProperty("items")[0].GetProperty("interestedPreview").EnumerateArray()); Assert.DoesNotContain("location", json.GetRawText());
    }
}

using System.Text.Json;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Application.Works;
using Nexo.Discovery.Domain;

namespace Nexo.Discovery.Unit;

public sealed class WorkRulesTests
{
    private static PublishWork Body => new("plomeria", "Trabajo terminado", "Reparamos la tubería de cobre y probamos el funcionamiento.", new(20.05, -99.34));
    [Theory]
    [InlineData(4, 20, false)]
    [InlineData(5, 20, true)]
    [InlineData(90, 1000, true)]
    [InlineData(91, 20, false)]
    [InlineData(5, 19, false)]
    [InlineData(5, 1001, false)]
    public void Limites_contractuales_de_titulo_y_texto(int title, int text, bool valid) =>
        Assert.Equal(valid, WorkRules.Validate(Body with { Title = new('t', title), Text = new('x', text) }, ["plomeria"]) == null);
    [Fact]
    public void Categoria_imagenes_y_coordenadas_no_se_confian_al_cliente()
    {
        Assert.Equal(NexoCodes.WorkCategoryNotInProfile, WorkRules.Validate(Body, ["pintura"]));
        Assert.Equal(NexoCodes.ContentImagesLimit, WorkRules.Validate(Body with { ImageUploadIds = Enumerable.Range(0, 7).Select(_ => Guid.NewGuid()).ToArray() }, ["plomeria"]));
        var duplicate = Guid.NewGuid();
        Assert.Equal(NexoCodes.ValidationFailed, WorkRules.Validate(Body with { ImageUploadIds = [duplicate, duplicate] }, ["plomeria"]));
        Assert.Equal(NexoCodes.ValidationFailed, WorkRules.Validate(Body with { Location = new(double.NaN, -99) }, ["plomeria"]));
        Assert.Equal(NexoCodes.ValidationFailed, WorkRules.Validate(Body with { Problem = new('p', 301) }, ["plomeria"]));
    }
    [Fact]
    public void Cursor_compacto_ligado_al_prestador_y_sin_offset()
    {
        var provider = Guid.CreateVersion7(); var work = Guid.CreateVersion7();
        var cursor = WorkRules.Cursor(work, provider);
        Assert.InRange(cursor.Length, 1, 64); Assert.True(WorkRules.TryCursor(cursor, provider, out var decoded)); Assert.Equal(work, decoded);
        Assert.False(WorkRules.TryCursor(cursor, Guid.NewGuid(), out _)); Assert.False(WorkRules.TryCursor(new('x', 65), provider, out _));
        Assert.False(WorkRules.TryCursor("invalid", provider, out _));
    }
    [Fact]
    public async Task Editar_trabajo_revalida_y_conserva_categoria_y_ubicacion()
    {
        var profiles = new Profiles(); var store = new Store(); var safety = new Safety();
        var id = Guid.NewGuid(); var now = DateTime.UtcNow;
        store.Value = new Work(id, profiles.Value.Profile.ProviderId, "plomeria", "Trabajo anterior",
            "Reparamos una tubería debajo del lavabo.", null, null, null, null,
            new(20.05, -99.34), "Zona", [], 3, now, now);
        var service = new WorkService(store, profiles, new Accounts(), safety, TimeProvider.System);
        var result = await service.EditAsync(profiles.Value.AccountId, id, JsonSerializer.SerializeToElement(new {
            title = "Trabajo corregido", text = "Reparamos la fuga y probamos la instalación.", tag = "Terminado", imageUploadIds = Array.Empty<Guid>()
        }), default);
        Assert.True(result.IsSuccess); Assert.Equal("plomeria", result.Value!.Category);
        Assert.Equal(3, result.Value.Recs); Assert.Equal(1, store.Writes); Assert.Equal(1, safety.Calls);
        Assert.Equal(NexoCodes.ContentImagesLimit, WorkRules.ValidateEdit(new("Trabajo nuevo", "Realizamos una instalación completa.", ImageUploadIds: Enumerable.Repeat(Guid.NewGuid(), 7).ToArray())));
    }
    private sealed class Accounts : IProviderAccounts
    {
        public bool Active = true;
        public Task<ProviderAccountState?> ReadAsync(Guid id, CancellationToken ct) => Task.FromResult<ProviderAccountState?>(new(Active, true, false));
        public Task<bool> EnableProviderAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Profiles : IProviderStore
    {
        public StoredProvider Value = new(Guid.NewGuid(), new() { ProviderId = Guid.CreateVersion7(), Categories = ["plomeria"] }, 0, "", "", true);
        public Task<StoredProvider?> OwnAsync(Guid id, CancellationToken ct) => Task.FromResult<StoredProvider?>(Value);
        public Task<IReadOnlyList<StoredProvider>> PublicAsync(Guid[] ids, ProviderPoint? from, CancellationToken ct) => Task.FromResult<IReadOnlyList<StoredProvider>>([Value]);
        public Task<bool> InsertAsync(StoredProvider p, CancellationToken ct) => throw new NotSupportedException();
        public Task RecordViewAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> PatchAsync(StoredProvider p, ProviderProfile v, string s, string[] fields, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class Safety : IProviderSafety
    {
        public string? Error;
        public string? Checked;
        public bool Approved = true;
        public int Calls;
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct)
        {
            Calls++; Checked = text;
            return Task.FromResult(Error == null ? Result<bool>.Success(Approved) : Result<bool>.Failure(Error, "No disponible"));
        }
        public Task<Result<ProviderImage>> ImageAsync(Guid owner, Guid id, string purpose, CancellationToken ct) => Task.FromResult(Result<ProviderImage>.Success(new("https://cdn.test/image.webp", null, 1, 1)));
    }
    private sealed class Store : IWorkStore
    {
        public Work? Value;
        public WorkReceipt? Receipt;
        public int Writes;
        public Task<WorkReceipt?> ReceiptAsync(Guid owner, Guid key, CancellationToken ct) => Task.FromResult(Receipt);
        public Task<Work?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(Value);
        public Task<Result<Work>> CreateAsync(Guid owner, Guid key, string hash, Work value, CancellationToken ct)
        {
            Writes++; Value = value; Receipt = new(hash, value.WorkId); return Task.FromResult(Result<Work>.Success(value));
        }
        public Task<Result<Work>> UpdateAsync(Guid owner, Guid id, Work value, CancellationToken ct)
        { Writes++; Value = value; return Task.FromResult(Result<Work>.Success(value)); }
        public Task<WorkPage> ListAsync(Guid id, Guid? after, int limit, Guid? viewer, CancellationToken ct) => throw new NotSupportedException();
        public Task<Result<bool>> DeleteAsync(Guid owner, Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Result<WorkRecommendation>> RecommendAsync(Guid viewer, Guid id, bool desired, CancellationToken ct) => throw new NotSupportedException();
    }
    [Theory]
    [InlineData(NexoCodes.ContentInappropriate)]
    [InlineData(NexoCodes.ServerDependencyUnavailable)]
    public async Task Moderacion_rechazada_o_caida_no_escribe(string error)
    {
        var store = new Store(); var safety = new Safety { Error = error }; var profiles = new Profiles();
        var service = new WorkService(store, profiles, new Accounts(), safety, TimeProvider.System);
        var result = await service.PublishAsync(profiles.Value.AccountId, Guid.NewGuid().ToString(), JsonSerializer.SerializeToElement(Body), default);
        Assert.Equal(error, result.Error!.Code); Assert.Equal(0, store.Writes);
    }
    [Fact]
    public async Task Moderar_campos_opcionales_difuminar_una_vez_y_reintentar_sin_duplicar()
    {
        var store = new Store(); var safety = new Safety(); var profiles = new Profiles();
        var service = new WorkService(store, profiles, new Accounts(), safety, TimeProvider.System);
        var input = Body with { Problem = "problema", Did = "solución", Time = "duración", Tag = "etiqueta" };
        var body = JsonSerializer.SerializeToElement(input); var key = Guid.NewGuid().ToString();
        var first = await service.PublishAsync(profiles.Value.AccountId, key, body, default);
        Assert.True(first.IsSuccess); Assert.Equal(1, store.Writes);
        foreach (var field in new[] { input.Title, input.Text, input.Problem, input.Did, input.Time, input.Tag }) Assert.Contains(field!, safety.Checked);
        Assert.InRange(input.Location.DistanceKm(first.Value!.PublicPoint), .499, .801);
        var second = await service.PublishAsync(profiles.Value.AccountId, key, body, default);
        Assert.Equal(first.Value, second.Value); Assert.Equal(1, safety.Calls); Assert.Equal(1, store.Writes);
        var changed = await service.PublishAsync(profiles.Value.AccountId, key, JsonSerializer.SerializeToElement(input with { Text = input.Text + " Cambio." }), default);
        Assert.Equal(NexoCodes.RequestIdempotencyKeyReused, changed.Error!.Code);
        Assert.DoesNotContain("location", JsonSerializer.Serialize(store.Value, ProviderService.Json), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task Imagen_aprobada_no_se_publica_sin_ciclo_real_de_purga()
    {
        var store = new Store(); var profiles = new Profiles();
        var service = new WorkService(store, profiles, new Accounts(), new Safety(), TimeProvider.System);
        var result = await service.PublishAsync(profiles.Value.AccountId, Guid.NewGuid().ToString(), JsonSerializer.SerializeToElement(Body with { ImageUploadIds = [Guid.NewGuid()] }), default);
        Assert.Equal(NexoCodes.ServerDependencyUnavailable, result.Error!.Code); Assert.Equal(0, store.Writes);
    }
    [Fact]
    public async Task Doble_con_aprobacion_false_no_aprueba_contenido_y_cuenta_inactiva_no_publica()
    {
        var store = new Store(); var profiles = new Profiles(); var accounts = new Accounts();
        var service = new WorkService(store, profiles, accounts, new Safety { Approved = false }, TimeProvider.System);
        var body = JsonSerializer.SerializeToElement(Body);
        Assert.Equal(NexoCodes.ContentInappropriate, (await service.PublishAsync(profiles.Value.AccountId, Guid.NewGuid().ToString(), body, default)).Error!.Code);
        accounts.Active = false;
        Assert.Equal(NexoCodes.AccountPendingDeletion, (await service.PublishAsync(profiles.Value.AccountId, Guid.NewGuid().ToString(), body, default)).Error!.Code);
        Assert.Equal(0, store.Writes);
    }
}

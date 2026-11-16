using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Engagement.Application.Orders;

namespace Nexo.Engagement.Unit;

/// <summary>Order ↔ Discovery request: assignment race compensation and reopen/close after transitions.</summary>
public sealed class OrderRequestLinkTests
{
    private static readonly Guid Client = Guid.CreateVersion7(), ProviderAccount = Guid.CreateVersion7(),
        Provider = Guid.CreateVersion7(), Request = Guid.CreateVersion7();

    private static OrderService.CreateOrderBody Post() =>
        new(Provider, Request, "post", "Fuga debajo del lavabo del baño.", "shop", null, null, null);

    [Fact]
    public async Task Solicitud_ajena_o_inexistente_da_404_sin_crear_pedido()
    {
        var f = new Fakes { State = OrderRequestState.Missing };
        var r = await f.Service.CreateAsync(Client, null, Guid.NewGuid().ToString(), Post(), default);
        Assert.Equal(NexoCodes.RequestNotFound, r.Error!.Code);
        Assert.Null(f.Created);
    }

    [Fact]
    public async Task Carrera_perdida_en_la_asignacion_revierte_el_pedido_y_da_409()
    {
        var f = new Fakes { AssignWins = false };
        var r = await f.Service.CreateAsync(Client, null, Guid.NewGuid().ToString(), Post(), default);
        Assert.Equal(NexoCodes.OrderRequestAlreadyAssigned, r.Error!.Code);
        Assert.Equal(f.Created!.Id, f.Deleted);
    }

    [Fact]
    public async Task Asignacion_ganada_crea_201()
    {
        var f = new Fakes();
        var r = await f.Service.CreateAsync(Client, null, Guid.NewGuid().ToString(), Post(), default);
        Assert.Equal(201, r.Value!.StatusCode);
        Assert.Null(f.Deleted);
        Assert.Equal(1, f.Assigns);
    }

    [Theory]
    [InlineData("plomeria", "plomeria")]
    [InlineData("jardineria", "plomeria")]
    [InlineData(null, "plomeria")]
    public async Task Categoria_solo_acepta_las_que_ofrece_el_prestador(string? sent, string stored)
    {
        var f = new Fakes();
        await f.Service.CreateAsync(Client, null, Guid.NewGuid().ToString(), Post() with { Category = sent }, default);
        Assert.Equal(stored, f.Created!.Category);
    }

    [Theory]
    [InlineData("rejected", true, false)]
    [InlineData("resolved", false, true)]
    public async Task Rechazo_reabre_y_resolucion_cierra_la_solicitud(string target, bool released, bool completed)
    {
        var f = new Fakes();
        f.Existing = f.Snapshot(target == "resolved" ? "accepted" : "pending");
        var r = await f.Service.TransitionAsync(ProviderAccount, null, f.Existing.Id, target, null, null, default);
        Assert.True(r.IsSuccess);
        Assert.Equal(released, f.Released);
        Assert.Equal(completed, f.Completed);
    }

    [Fact]
    public async Task Cancelacion_del_cliente_no_reabre_la_solicitud()
    {
        var f = new Fakes();
        f.Existing = f.Snapshot("pending");
        await f.Service.TransitionAsync(Client, null, f.Existing.Id, "cancelled", null, null, default);
        Assert.False(f.Released);
    }

    private sealed class Fakes : IOrderStore, IOrderProviderAccess, IOrderAddressProtection, IOrderSafety, IOrderRequestLink
    {
        public OrderRequestState State = OrderRequestState.Open;
        public bool AssignWins = true, Released, Completed;
        public int Assigns;
        public OrderSnapshot? Created, Existing;
        public Guid? Deleted;
        public OrderService Service => new(this, this, this, this, this, null!, TimeProvider.System);

        public OrderSnapshot Snapshot(string status) => new(Guid.CreateVersion7(), Client, Provider, Request, "post", "", "Fuga debajo del lavabo.",
            "shop", "Centro", null, null, status, DateTime.UtcNow, null, null, null, null, 1, null, null);

        public Task<OrderSnapshot?> CreateAsync(OrderDraft d, Guid key, string hash, CancellationToken ct) =>
            Task.FromResult<OrderSnapshot?>(Created = Snapshot("pending") with { Id = d.Id, RequestId = d.RequestId, Category = d.Category });
        public Task DeleteCreatedAsync(Guid id, CancellationToken ct) { Deleted = id; return Task.CompletedTask; }
        public Task<OrderSnapshot?> FindAsync(Guid id, Guid participant, CancellationToken ct) => Task.FromResult(Existing);
        public Task<OrderMutationResult> TransitionAsync(Guid id, Guid actor, Guid? provider, string target, int? v, OrderMutation m, CancellationToken ct) =>
            Task.FromResult(new OrderMutationResult(Existing! with { Status = target, Version = 2 }, false, false, false));
        public Task<OrderSnapshot?> FindByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderPage> ListAsync(Guid a, Guid? p, string r, string s, Guid? b, int l, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderMutationResult> ChangeAddressAsync(Guid id, Guid c, int? v, OrderAddress a, string cipher, string z, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderEvent[]> EventsAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();

        public Task<OrderProvider?> GetAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<OrderProvider?>(new(Provider, ProviderAccount, "Plomería", ["plomeria"], "both", true, "Centro"));
        public Task<OrderClient?> GetClientAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid?> ProviderForAccountAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();

        public Task<string> EncryptAsync(OrderAddress a, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderAddress> DecryptAsync(string c, CancellationToken ct) => throw new NotSupportedException();
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(Result<bool>.Success(true));

        public Task<OrderRequestState> StateAsync(Guid c, Guid r, CancellationToken ct) => Task.FromResult(State);
        public Task<bool> AssignAsync(Guid c, Guid r, Guid p, CancellationToken ct) { Assigns++; return Task.FromResult(AssignWins); }
        public Task<bool> ReleaseAsync(Guid r, Guid p, CancellationToken ct) { Released = true; return Task.FromResult(true); }
        public Task<bool> CompleteAsync(Guid r, Guid p, DateTime now, CancellationToken ct) { Completed = true; return Task.FromResult(true); }
    }
}

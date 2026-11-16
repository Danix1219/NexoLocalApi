using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Engagement.Application.Orders;
using Nexo.Engagement.Application.Reviews;

namespace Nexo.Engagement.Unit;

public sealed class ReviewServiceTests
{
    private static readonly Guid Client = Guid.CreateVersion7(), Provider = Guid.CreateVersion7(), OrderId = Guid.CreateVersion7();

    [Theory]
    [InlineData("Ana López Ruiz", "Ana L.")]
    [InlineData("Ana", "Ana")]
    [InlineData("  ", "Usuario")]
    [InlineData(null, "Usuario")]
    public void Autor_publico_solo_nombre_e_inicial(string? full, string expected) => Assert.Equal(expected, ReviewService.ShortName(full));

    [Theory]
    [InlineData("resolved", null, 0)]
    [InlineData("accepted", NexoCodes.ReviewOrderNotResolved, 0)]
    public async Task Solo_pedidos_resueltos_admiten_opinion(string status, string? error, int _)
    {
        var f = new Fakes { Status = status };
        var r = await f.Service.CreateAsync(Client, Guid.NewGuid().ToString(), new(OrderId, 5, "Llegó puntual y dejó todo limpio."), default);
        Assert.Equal(error, r.Error?.Code);
        if (error == null) { Assert.Equal(201, r.Value.Status); Assert.Equal((Provider, 5.0, 1), f.Rating); }
    }

    [Fact]
    public async Task Pedido_de_otro_cliente_da_404()
    {
        var f = new Fakes();
        var r = await f.Service.CreateAsync(Guid.CreateVersion7(), Guid.NewGuid().ToString(), new(OrderId, 4), default);
        Assert.Equal(NexoCodes.ResourceNotFound, r.Error!.Code);
    }

    [Theory]
    [InlineData(0, null, NexoCodes.ReviewStarsInvalid)]
    [InlineData(6, null, NexoCodes.ReviewStarsInvalid)]
    [InlineData(5, "corto", NexoCodes.ValidationFailed)]
    public async Task Valida_estrellas_y_texto(int stars, string? text, string code)
    {
        var r = await new Fakes().Service.CreateAsync(Client, Guid.NewGuid().ToString(), new(OrderId, stars, text), default);
        Assert.Equal(code, r.Error!.Code);
    }

    [Fact]
    public async Task Segunda_opinion_del_mismo_pedido_da_409_y_reintento_identico_200()
    {
        var f = new Fakes();
        var key = Guid.NewGuid().ToString();
        await f.Service.CreateAsync(Client, key, new(OrderId, 5), default);
        Assert.Equal(200, (await f.Service.CreateAsync(Client, key, new(OrderId, 5), default)).Value.Status);
        Assert.Equal(NexoCodes.ReviewAlreadyExists, (await f.Service.CreateAsync(Client, Guid.NewGuid().ToString(), new(OrderId, 3), default)).Error!.Code);
    }

    [Fact]
    public async Task Tope_diario_de_20()
    {
        var f = new Fakes { Today = ReviewService.DailyLimit };
        var r = await f.Service.CreateAsync(Client, Guid.NewGuid().ToString(), new(OrderId, 5), default);
        Assert.Equal(NexoCodes.RateLimitExceeded, r.Error!.Code);
    }

    private sealed class Fakes : IReviewStore, IOrderStore, IOrderProviderAccess, IOrderSafety, IReviewDirectory
    {
        public string Status = "resolved";
        public int Today;
        public (Guid, double, int)? Rating;
        private readonly List<StoredReview> rows = [];
        public ReviewService Service => new(this, this, this, this, this, TimeProvider.System);

        public Task<StoredReview?> ByOrderAsync(Guid id, CancellationToken ct) => Task.FromResult(rows.FirstOrDefault(r => r.OrderId == id));
        public Task<int> CountSinceAsync(Guid c, DateTime s, CancellationToken ct) => Task.FromResult(Today);
        public Task<bool> InsertAsync(StoredReview r, CancellationToken ct) { rows.Add(r); return Task.FromResult(true); }
        public Task<ReviewSummary> SummaryAsync(Guid p, CancellationToken ct) =>
            Task.FromResult(new ReviewSummary(rows.Count == 0 ? 0 : rows.Average(r => r.Stars), rows.Count, 0, new Dictionary<string, int>()));
        public Task<StoredReview[]> PageAsync(Guid p, Guid? b, int l, CancellationToken ct) => Task.FromResult(rows.ToArray());

        public Task<OrderSnapshot?> FindAsync(Guid id, Guid participant, CancellationToken ct) =>
            Task.FromResult<OrderSnapshot?>(participant != Client ? null : new(OrderId, Client, Provider, null, "direct", "plomeria", "Fuga en el baño.",
                "shop", "Centro", null, null, Status, DateTime.UtcNow, null, null, null, null, 3, null, null));
        public Task<OrderSnapshot?> FindByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderSnapshot?> CreateAsync(OrderDraft d, Guid k, string h, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderPage> ListAsync(Guid a, Guid? p, string r, string s, Guid? b, int l, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderMutationResult> TransitionAsync(Guid id, Guid a, Guid? p, string t, int? v, OrderMutation m, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderMutationResult> ChangeAddressAsync(Guid id, Guid c, int? v, OrderAddress a, string ci, string z, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderEvent[]> EventsAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task DeleteCreatedAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();

        public Task<OrderProvider?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<OrderProvider?>(null);
        public Task<OrderClient?> GetClientAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Guid?> ProviderForAccountAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Result<bool>> ModerateAsync(string t, CancellationToken ct) => Task.FromResult(Result<bool>.Success(true));

        public Task<IReadOnlyDictionary<Guid, ReviewAuthor>> AuthorsAsync(Guid[] ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<Guid, ReviewAuthor>>(new Dictionary<Guid, ReviewAuthor> { [Client] = new("Ana López", null) });
        public Task SetRatingAsync(Guid p, double avg, int count, CancellationToken ct) { Rating = (p, avg, count); return Task.CompletedTask; }
    }
}

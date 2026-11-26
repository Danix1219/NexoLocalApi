using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Assist;
using Nexo.Discovery.Application.Providers;
using Nexo.Discovery.Domain;
using Nexo.Discovery.Infrastructure.Assist;

namespace Nexo.Discovery.Unit;

public sealed class AssistTests
{
    private static readonly Guid Account = Guid.NewGuid();
    private static AssistService.DraftBody Body(string text = "se me rompió la llave del lavabo") => new("request", "plomeria", text, 600);

    [Theory]
    [InlineData("Llámame al 771 555 0100", "[dato omitido]")]
    [InlineData("escribe a ana@correo.mx", "[dato omitido]")]
    [InlineData("mira https://ejemplo.com/foto", "[dato omitido]")]
    public void Quita_telefonos_correos_y_enlaces(string input, string expected) => Assert.Contains(expected, AssistService.Redact(input));

    [Fact]
    public async Task Borrador_valido_y_sin_datos_de_contacto()
    {
        var f = new Fakes { Draft = new("Necesito un plomero. Mi teléfono es 7715550100.", "Fuga en el lavabo", "gpt-4o-mini") };
        var r = await f.Service.DraftAsync(Account, Body(), default);
        Assert.True(r.IsSuccess);
        var json = System.Text.Json.JsonSerializer.Serialize(r.Value);
        Assert.Contains("Fuga en el lavabo", json);
        Assert.DoesNotContain("7715550100", json);
        Assert.Contains("gpt-4o-mini", json);
    }

    [Theory]
    [InlineData("otro", "plomeria", "texto suficiente")]
    [InlineData("request", "Plomería!", "texto suficiente")]
    [InlineData("request", "plomeria", "ab")]
    public async Task Valida_la_entrada(string kind, string category, string text)
    {
        var r = await new Fakes().Service.DraftAsync(Account, new(kind, category, text, 600), default);
        Assert.Equal(NexoCodes.ValidationFailed, r.Error!.Code);
    }

    [Fact]
    public async Task Entrada_ofensiva_403_sin_gastar_cupo_ni_llamar_al_modelo()
    {
        var f = new Fakes { RejectContaining = "pendejo" };
        Assert.Equal(NexoCodes.ContentInappropriate, (await f.Service.DraftAsync(Account, Body("eres un pendejo"), default)).Error!.Code);
        Assert.Equal(0, f.ModelCalls);
        Assert.Equal(0, f.QuotaCalls);
    }

    [Fact]
    public async Task Salida_ofensiva_o_modelo_caido_es_no_disponible()
    {
        Assert.Equal(NexoCodes.AssistUnavailable, (await new Fakes { Draft = null }.Service.DraftAsync(Account, Body(), default)).Error!.Code);
        var bad = new Fakes { Draft = new("texto con pendejo", null, "m"), RejectContaining = "pendejo" };
        Assert.Equal(NexoCodes.AssistUnavailable, (await bad.Service.DraftAsync(Account, Body(), default)).Error!.Code);
    }

    [Fact]
    public async Task Cupo_diario_agotado_429()
    {
        var r = await new Fakes { QuotaLeft = false }.Service.DraftAsync(Account, Body(), default);
        Assert.Equal(NexoCodes.RateLimitExceeded, r.Error!.Code);
    }

    [Fact]
    public async Task Cliente_Azure_OpenAI_arma_la_peticion_y_lee_el_json()
    {
        var handler = new Handler("""{"model":"gpt-4o-mini","choices":[{"message":{"content":"{\"title\":\"Fuga\",\"text\":\"Necesito reparar la llave.\"}"}}]}""");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://nexo.openai.azure.com/",
            ["AzureOpenAI:ApiKey"] = "k",
            ["AzureOpenAI:Deployment"] = "chat"
        }).Build();
        var draft = await new AzureOpenAiAssistModel(new HttpClient(handler), config, NullLogger<AzureOpenAiAssistModel>.Instance)
            .DraftAsync("request", "plomeria", "llave rota", 600, default);
        Assert.Equal("Necesito reparar la llave.", draft!.Text);
        Assert.Equal("Fuga", draft.Title);
        Assert.Contains("/openai/deployments/chat/chat/completions", handler.Uri);
        Assert.Equal("k", handler.Key);
        // Without configuration it stays off instead of failing.
        Assert.Null(await new AzureOpenAiAssistModel(new HttpClient(handler), new ConfigurationBuilder().Build(), NullLogger<AzureOpenAiAssistModel>.Instance)
            .DraftAsync("request", "plomeria", "llave rota", 600, default));
    }

    private sealed class Handler(string body) : HttpMessageHandler
    {
        public string Uri { get; private set; } = "";
        public string? Key { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Uri = request.RequestUri!.ToString();
            Key = request.Headers.TryGetValues("api-key", out var v) ? v.Single() : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class Fakes : IAssistModel, IAssistQuota, IProviderSafety
    {
        public AssistDraft? Draft = new("Necesito reparar una fuga en el lavabo.", "Fuga en el lavabo", "m");
        public string? RejectContaining;
        public bool QuotaLeft = true;
        public int ModelCalls, QuotaCalls;
        public AssistService Service => new(this, this, this, TimeProvider.System);

        public Task<AssistDraft?> DraftAsync(string kind, string category, string userText, int limit, CancellationToken ct) { ModelCalls++; return Task.FromResult(Draft); }
        public Task<bool> TryConsumeAsync(Guid account, DateTime today, int limit, CancellationToken ct) { QuotaCalls++; return Task.FromResult(QuotaLeft); }
        public Task<Result<bool>> ModerateAsync(string text, CancellationToken ct) => Task.FromResult(RejectContaining != null && text.Contains(RejectContaining)
            ? Result<bool>.Failure(NexoCodes.ContentInappropriate, "no") : Result<bool>.Success(true));
        public Task<Result<ProviderImage>> ImageAsync(Guid o, Guid u, string p, CancellationToken ct) => throw new NotSupportedException();
    }
}

using System.Text.RegularExpressions;
using Nexo.Common.Errors;
using Nexo.Common.Results;
using Nexo.Discovery.Application.Providers;

namespace Nexo.Discovery.Application.Assist;

public sealed record AssistDraft(string Text, string? Title, string ModelVersion);

/// <summary>Generative model behind EP-082. Returns null when it is not configured or fails: the person writes by hand.</summary>
public interface IAssistModel
{
    Task<AssistDraft?> DraftAsync(string kind, string category, string userText, int lengthLimit, CancellationToken ct);
}

/// <summary>Daily per-account quota; false once the day's allowance is used.</summary>
public interface IAssistQuota
{
    Task<bool> TryConsumeAsync(Guid account, DateTime today, int limit, CancellationToken ct);
}

/// <summary>EP-082 `POST /v1/assist/draft`: an editable draft, never published by itself. Text is not stored.</summary>
public sealed partial class AssistService(IAssistModel model, IAssistQuota quota, IProviderSafety safety, TimeProvider clock)
{
    public const int DailyLimit = 20;

    public async Task<Result<object>> DraftAsync(Guid account, DraftBody? body, CancellationToken ct)
    {
        var text = body?.UserText?.Trim();
        var limit = body?.LengthLimit ?? 600;
        if (body?.Kind is not ("request" or "work") || body.CategoryId is not { } category || !CategoryPattern().IsMatch(category) ||
            text is not { Length: >= 3 and <= 1000 } || limit is < 100 or > 1000) return Fail(NexoCodes.ValidationFailed);

        // Re-redact on the server: the app already did, but the boundary does not trust the client.
        var clean = Redact(text);
        var input = await safety.ModerateAsync(clean, ct);
        // Offensive input is the person's fault (403); moderation down means the assistant is down (503).
        if (input.IsFailure) return input.Error!.Code == NexoCodes.ContentInappropriate ? Result<object>.Failure(input.Error) : Fail(NexoCodes.AssistUnavailable);
        if (!await quota.TryConsumeAsync(account, clock.GetUtcNow().UtcDateTime.Date, DailyLimit, ct)) return Fail(NexoCodes.RateLimitExceeded);

        var draft = await model.DraftAsync(body.Kind!, category, clean, limit, ct);
        if (draft == null || string.IsNullOrWhiteSpace(draft.Text)) return Fail(NexoCodes.AssistUnavailable);
        var output = Trim(Redact(draft.Text), limit);
        // The model's text is moderated too; a rejected draft is "unavailable", not the person's fault.
        var check = await safety.ModerateAsync(output + " " + draft.Title, ct);
        if (check.IsFailure) return Fail(check.Error!.Code == NexoCodes.ContentInappropriate ? NexoCodes.AssistUnavailable : check.Error.Code);
        return Result<object>.Success(new
        {
            draft = new { text = output, title = draft.Title is { Length: > 0 } t ? Trim(Redact(t), 80) : null },
            modelVersion = draft.ModelVersion
        });
    }

    /// <summary>Drops phones, e-mails and links: a draft is public text and must not carry contact data.</summary>
    public static string Redact(string text) =>
        Url().Replace(Email().Replace(Phone().Replace(text, "[dato omitido]"), "[dato omitido]"), "[dato omitido]").Trim();

    private static string Trim(string text, int max) => text.Length <= max ? text : text[..max].TrimEnd();
    private static Result<object> Fail(string code) => Result<object>.Failure(code, "El asistente no pudo generar el borrador.");

    [GeneratedRegex(@"^[a-z0-9_-]{2,30}$")] private static partial Regex CategoryPattern();
    [GeneratedRegex(@"\+?\d[\d\s\-().]{6,}\d")] private static partial Regex Phone();
    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+")] private static partial Regex Email();
    [GeneratedRegex(@"(https?://|www\.)\S+", RegexOptions.IgnoreCase)] private static partial Regex Url();

    public sealed record DraftBody(string? Kind = null, string? CategoryId = null, string? UserText = null, int? LengthLimit = null);
}

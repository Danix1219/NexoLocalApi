using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nexo.Common.Errors;
using Nexo.Common.RateLimiting;
using Nexo.Identity.Application.Features;
using Nexo.Identity.Application.Ports;

namespace Nexo.Identity.Endpoints.Http;

public sealed record AccountResponse(Guid AccountId, string Email, DateTime EmailVerifiedAt, string Name,
    string? PhotoUrl, string CurrentMode, bool HasProvider, string? Phone, string Status,
    DateTime? DeletionRequestedAt, DateTime CreatedAt);
public sealed record ChangeModeBody(string? Mode);
public sealed record DeclarePhoneBody(string? Phone);
public sealed record RegisterDeviceBody(string? FcmToken, string? Platform, string? AppVersion);
public sealed record RequestDeletionBody(string? Password, string? Reason);
public sealed record DeviceResponse(Guid DeviceId, DateTime LastSeenAt);
public sealed record DeletionResponse(string Status, DateTime DeletionRequestedAt, DateTime EffectiveAt, int DaysRemaining, bool CanCancel);

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this RouteGroupBuilder group)
    {
        var own = group.MapGroup("").RequireAuthorization().RequireRateLimiting(NexoRateLimits.User);
        Document(own.MapGet("/me", async (HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "private, no-store";
            var result = await service.GetAsync(UserId(http), ct);
            return result.Match<IResult>(value => Results.Ok(ToResponse(value)), error => NexoProblem.From(error, http));
        })).Produces<AccountResponse>();

        Document(own.MapPatch("/me", async (JsonElement body, HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            if (!TryPatch(body, out var patch))
                return NexoProblem.From(NexoCodes.ValidationFailed, "Envia nombre o foto con el formato permitido.", http);
            var result = await service.PatchAsync(UserId(http), patch!, ct);
            return result.Match<IResult>(value => Results.Ok(ToResponse(value)), error => NexoProblem.From(error, http));
        })).Produces<AccountResponse>();

        Document(own.MapPost("/me/mode", async (ChangeModeBody body, HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            var result = await service.ChangeModeAsync(UserId(http), body.Mode, http.User.FindFirst(TokenEndpoint.ModeClaim)?.Value, ct);
            if (result.Error?.Code == NexoCodes.ProviderProfileRequired)
                return ExtendedProblem(NexoCodes.ProviderProfileRequired, http, new Dictionary<string, object?> { ["nextStep"] = "createProvider" });
            return result.Match<IResult>(Results.Ok, error => NexoProblem.From(error, http));
        })).Produces<ModeChanged>();

        Document(own.MapPost("/me/phone", async (DeclarePhoneBody body, HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            var result = await service.SetPhoneAsync(UserId(http), body.Phone, ct);
            return result.Match<IResult>(Results.Ok, error => NexoProblem.From(error, http));
        })).Produces<PhoneDeclared>();

        Document(own.MapPost("/devices", async (RegisterDeviceBody body, HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            var result = await service.RegisterDeviceAsync(UserId(http), body.FcmToken, body.Platform, body.AppVersion, ct);
            return result.Match<IResult>(value => value.Created
                    ? Results.Created($"/v1/identity/devices/{value.DeviceId}", new DeviceResponse(value.DeviceId, value.LastSeenAt))
                    : Results.Ok(new DeviceResponse(value.DeviceId, value.LastSeenAt)),
                error => NexoProblem.From(error, http));
        })).Produces<DeviceResponse>(201).Produces<DeviceResponse>();

        Document(own.MapDelete("/devices/{id}", async (string id, HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var deviceId))
                return NexoProblem.From(NexoCodes.ValidationFailed, "Identificador invalido.", http);
            var result = await service.DeleteDeviceAsync(UserId(http), deviceId, ct);
            return result.Match<IResult>(_ => Results.NoContent(), error => NexoProblem.From(error, http));
        })).Produces(204);

        Document(own.MapPost("/me/deletion", async (RequestDeletionBody body, HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            var result = await service.RequestDeletionAsync(UserId(http), body.Password, body.Reason, ct);
            return result.Match<IResult>(value => value.AlreadyPending
                    ? ExtendedProblem(NexoCodes.AccountAlreadyPendingDeletion, http, new Dictionary<string, object?>
                    { ["deletionRequestedAt"] = value.DeletionRequestedAt, ["effectiveAt"] = value.EffectiveAt })
                    : Results.Accepted(value: new DeletionResponse(
                        value.Status, value.DeletionRequestedAt, value.EffectiveAt, value.DaysRemaining, value.CanCancel)),
                error => NexoProblem.From(error, http));
        })).Produces<DeletionResponse>(202);

        Document(own.MapDelete("/me/deletion", async (HttpContext http, AccountManagement service, CancellationToken ct) =>
        {
            var result = await service.CancelDeletionAsync(UserId(http), ct);
            return result.Match<IResult>(Results.Ok, error => NexoProblem.From(error, http));
        })).Produces<DeletionCancelled>().ProducesProblem(410);
        return group;
    }

    private static RouteHandlerBuilder Document(RouteHandlerBuilder route) => route
        .ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404)
        .ProducesProblem(409).ProducesProblem(429).ProducesProblem(503);

    private static AccountResponse ToResponse(AccountView a) => new(a.Id, a.Email, a.EmailVerifiedAt,
        a.Name, a.PhotoUrl, a.CurrentMode, a.HasProvider, a.Phone, a.Status, a.DeletionRequestedAt, a.CreatedAt);

    private static Guid UserId(HttpContext http) => Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;

    private static bool TryPatch(JsonElement body, out AccountPatch? patch)
    {
        patch = null;
        if (body.ValueKind != JsonValueKind.Object) return false;
        var fields = new HashSet<string>(StringComparer.Ordinal);
        string? name = null;
        Guid? upload = null;
        foreach (var property in body.EnumerateObject())
        {
            if (!fields.Add(property.Name)) return false;
            if (property.Name == "name" && property.Value.ValueKind == JsonValueKind.String) name = property.Value.GetString();
            else if (property.Name == "photoUploadId")
            {
                if (property.Value.ValueKind == JsonValueKind.Null) continue;
                if (property.Value.ValueKind != JsonValueKind.String || !property.Value.TryGetGuid(out var id)) return false;
                upload = id;
            }
            else return false;
        }
        patch = new AccountPatch(fields.Contains("name"), name, fields.Contains("photoUploadId"), upload);
        return true;
    }

    private static IResult ExtendedProblem(string code, HttpContext http, Dictionary<string, object?> extensions)
    {
        var (status, title) = NexoProblem.Describe(code);
        extensions["code"] = code;
        extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? http.TraceIdentifier;
        return Results.Problem(statusCode: status, title: title, instance: http.Request.Path,
            type: "https://nexolocal.mx/problems/" + code.Replace('.', '-').Replace('_', '-'), extensions: extensions);
    }
}

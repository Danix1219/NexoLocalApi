using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Nexo.Common.Mongo;
using Nexo.Engagement.Application.Notifications;

namespace Nexo.Engagement.Infrastructure.Notifications;

/// <summary>FCM push to every registered device of the account. Enabled only with Firebase__CredentialsJson
/// (service account JSON, a secret: App Service setting or local .env, never the app).</summary>
public sealed class FcmPushSender : IPushSender
{
    private readonly FirebaseMessaging? messaging;
    private readonly IMongoCollection<BsonDocument> devices;
    private readonly ILogger<FcmPushSender> log;

    public FcmPushSender(IConfiguration configuration, [FromKeyedServices(NexoDatabaseKeys.Identity)] IMongoDatabase identity, ILogger<FcmPushSender> log)
    {
        this.log = log;
        devices = identity.GetCollection<BsonDocument>("devices");
        var credentials = configuration["Firebase:CredentialsJson"];
        if (string.IsNullOrWhiteSpace(credentials)) return;
        var app = FirebaseApp.GetInstance("nexo-push") ?? FirebaseApp.Create(new AppOptions
        {
            Credential = CredentialFactory.FromJson<ServiceAccountCredential>(credentials).ToGoogleCredential()
        }, "nexo-push");
        messaging = FirebaseMessaging.GetMessaging(app);
    }

    public bool Enabled => messaging != null;

    public async Task SendAsync(Guid account, string title, string body, string? deepLink, CancellationToken ct)
    {
        if (messaging == null) return;
        var tokens = await devices.Find(new BsonDocument("accountId", new BsonBinaryData(account, GuidRepresentation.Standard)))
            .Project(new BsonDocument("fcmToken", 1)).ToListAsync(ct);
        var list = tokens.Select(t => t.GetValue("fcmToken", "").AsString).Where(t => t.Length > 0).Distinct().Take(500).ToList();
        if (list.Count == 0) return;
        // The app registers FCM registration tokens (firebase_messaging getToken), which is what Tokens takes;
        // Fids are installation ids, a different identifier. Upgrade when the app migrates.
#pragma warning disable CS0618
        var result = await messaging.SendEachForMulticastAsync(new MulticastMessage
        {
            Tokens = list,
#pragma warning restore CS0618
            Notification = new Notification { Title = title, Body = body },
            Data = deepLink == null ? null : new Dictionary<string, string> { ["deepLink"] = deepLink }
        }, ct);
        // Tokens FCM says are dead (app uninstalled) are dropped so they stop costing calls.
        for (var i = 0; i < result.Responses.Count; i++)
            if (result.Responses[i].Exception?.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                await devices.DeleteOneAsync(new BsonDocument("fcmToken", list[i]), ct);
        if (result.FailureCount > 0) log.LogInformation("Push: {Failures} de {Total} no entregados.", result.FailureCount, list.Count);
    }
}

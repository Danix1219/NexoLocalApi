using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nexo.Common.Mongo;
using MongoDB.Driver;
using Nexo.Common.Errors;
using Nexo.Engagement.Application.Conversations;
using Nexo.Engagement.Infrastructure;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class ChatEncryptionPersistenceTests(InfraFixture infra)
{
    private static ChatDeviceKeys Bundle() => new(1, 1234,
        Curve(), 1, Curve(), Convert.ToBase64String(new byte[64]),
        Enumerable.Range(1, 20).Select(i => new ChatPreKey(i, Curve())).ToArray(),
        1, Convert.ToBase64String(new byte[1569]), Convert.ToBase64String(new byte[64]));
    private static string Curve() { var bytes = new byte[33]; bytes[0] = 5; return Convert.ToBase64String(bytes); }

    [Fact]
    public async Task Concurrent_claims_never_reuse_prekeys_and_registration_cannot_refill_or_replace_revoked_keys()
    {
        var client = new MongoClient(infra.MongoConnectionString);
        var name = "chat_keys_" + Guid.NewGuid().ToString("N");
        try
        {
            var keys = new MongoChatKeyStore(client.GetDatabase(name));
            var owner = Guid.NewGuid(); var bundle = Bundle();
            Assert.True(await keys.RegisterAsync(owner, bundle, default));
            var claimed = await Task.WhenAll(Enumerable.Range(0, 30).Select(_ => keys.ClaimAsync(owner, 1, default)));
            var ids = claimed.Where(k => k is not null).Select(k => k!.OneTimeKey!.Id).ToArray();
            Assert.Equal(20, ids.Length); Assert.Equal(20, ids.Distinct().Count());
            Assert.True(await keys.RegisterAsync(owner, bundle, default));
            Assert.Null(await keys.ClaimAsync(owner, 1, default));
            await keys.RevokeAsync(owner, 1, default);
            Assert.Empty(await keys.DevicesAsync(owner, default));
            Assert.False(await keys.RegisterAsync(owner, bundle, default));
        }
        finally { await client.DropDatabaseAsync(name); }
    }

    [Fact]
    public async Task Private_conversation_rejects_downgrade_omitted_devices_and_cross_account_access_and_tracks_delivery()
    {
        new ServiceCollection().AddMongoDatabases(new ConfigurationBuilder().Build());
        var mongo = new MongoClient(infra.MongoConnectionString);
        var name = "chat_private_" + Guid.NewGuid().ToString("N");
        try
        {
            var db = mongo.GetDatabase(name);
            var store = new MongoConversationStore(db); var keys = new MongoChatKeyStore(db);
            var me = Guid.NewGuid(); var peer = Guid.NewGuid(); var outsider = Guid.NewGuid();
            var (conversation, _) = await store.OpenAsync(me, Guid.NewGuid(), peer, null, DateTime.UtcNow, default);
            await keys.RegisterAsync(me, Bundle(), default); await keys.RegisterAsync(peer, Bundle(), default);
            var service = new EncryptedChatService(store, keys, new QuietLive(), TimeProvider.System);
            Assert.Equal(NexoCodes.ConversationNotFound, (await service.EnableAsync(outsider, conversation.Id, default)).Error!.Code);
            Assert.True((await service.EnableAsync(me, conversation.Id, default)).IsSuccess);
            var now = DateTime.UtcNow;
            var plain = new ChatMessage(Guid.CreateVersion7(), conversation.Id, me, "text", "must not persist", null, null, null, null, now, null);
            Assert.Null(await store.SendAsync(conversation, plain, "must not persist", Guid.NewGuid(), default));
            var operation = Guid.NewGuid();
            var envelope = new ChatCipherEnvelope(1, operation, 1, peer, 1, 3, Convert.ToBase64String(new byte[64]));
            Assert.Equal(NexoCodes.ValidationFailed, (await service.SendAsync(me, conversation.Id, operation.ToString(), new([envelope with { Recipient = outsider }]), default)).Error!.Code);
            var body = new EncryptedChatBody([envelope]);
            Assert.True((await service.SendAsync(me, conversation.Id, operation.ToString(), body, default)).IsSuccess);
            Assert.True((await service.SendAsync(me, conversation.Id, operation.ToString(), body, default)).IsSuccess);
            var rows = await store.MessagesAsync(conversation.Id, null, null, 30, default);
            Assert.Single(rows); Assert.Equal("encrypted", rows[0].Kind);
            Assert.Equal(1, await store.MarkDeliveredAsync(conversation, peer, rows[0].Id, now, default));
            Assert.Equal(0, await store.MarkDeliveredAsync(conversation, peer, rows[0].Id, now, default));
            Assert.NotNull((await store.MessagesAsync(conversation.Id, null, null, 30, default))[0].DeliveredAt);
            var target = rows[0].Id;
            var editKey = Guid.NewGuid();
            var edited = new EncryptedChatBody([envelope with { MessageId = editKey }]);
            Assert.True((await service.MutateAsync(me, conversation.Id, target, editKey.ToString(), edited, false, default)).IsSuccess);
            var revision = Assert.Single(await store.MessagesAsync(conversation.Id, null, null, 30, default));
            Assert.NotNull(revision.EditedAt);
            Assert.Equal(rows[0].CreatedAt, revision.CreatedAt);
            Assert.NotNull(revision.DeliveredAt);
            Assert.Equal("encrypted", revision.Kind);
            var nextKey = Guid.NewGuid();
            var nextBody = new EncryptedChatBody([envelope with { MessageId = nextKey }]);
            Assert.True((await service.MutateAsync(me, conversation.Id, target, nextKey.ToString(), nextBody, false, default)).IsSuccess);
            Assert.True((await service.MutateAsync(me, conversation.Id, target, editKey.ToString(), edited, false, default)).IsSuccess);
            Assert.Contains(nextKey.ToString(), Assert.Single(await store.MessagesAsync(conversation.Id, null, null, 30, default)).Text!);
            Assert.Equal(NexoCodes.ConversationNotFound, (await service.MutateAsync(peer, conversation.Id, target, Guid.NewGuid().ToString(), null, true, default)).Error!.Code);
            Assert.Equal(NexoCodes.ConversationNotFound, (await service.MutateAsync(outsider, conversation.Id, target, Guid.NewGuid().ToString(), null, true, default)).Error!.Code);
            Assert.Equal(NexoCodes.ValidationFailed, (await service.MutateAsync(me, conversation.Id, target, Guid.NewGuid().ToString(), new([]), false, default)).Error!.Code);
            var (other, _) = await store.OpenAsync(me, Guid.NewGuid(), peer, null, now, default);
            await store.EnableEncryptionAsync(other.Id, me, default);
            Assert.Equal(NexoCodes.ConversationNotFound, (await service.MutateAsync(me, other.Id, target, Guid.NewGuid().ToString(), null, true, default)).Error!.Code);
            Assert.True((await service.MutateAsync(me, conversation.Id, target, Guid.NewGuid().ToString(), null, true, default)).IsSuccess);
            var removed = Assert.Single(await store.MessagesAsync(conversation.Id, null, null, 30, default));
            Assert.Equal("deleted", removed.Kind);
            Assert.Null(removed.Text);
            Assert.Equal(NexoCodes.ConversationNotFound, (await service.MutateAsync(me, conversation.Id, target, editKey.ToString(), edited, false, default)).Error!.Code);
            Assert.True((await service.MutateAsync(me, conversation.Id, target, Guid.NewGuid().ToString(), null, true, default)).IsSuccess);
        }
        finally { await mongo.DropDatabaseAsync(name); }
    }

    private sealed class QuietLive : IChatRealtime
    { public Task SendAsync(Guid account, string method, object payload, CancellationToken ct) => Task.CompletedTask; }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Nexo.Common.Mongo;
using MongoDB.Driver;
using Nexo.Identity.Domain.Accounts;
using Nexo.Identity.Infrastructure.Persistence;

namespace Nexo.Integration;

[Collection("infra")]
public sealed class ProfileSharePersistenceTests(InfraFixture infra)
{
    [Fact]
    public async Task Shared_profile_requires_opt_in_is_minimal_and_can_be_revoked()
    {
        new ServiceCollection().AddMongoDatabases(new ConfigurationBuilder().Build());
        var db = new MongoClient(infra.MongoConnectionString).GetDatabase("shares_" + Guid.NewGuid().ToString("N"));
        var collections = new IdentityCollections(db);
        var owner = Guid.NewGuid();
        await collections.Accounts.InsertOneAsync(new Account
        {
            Id = owner,
            Email = "private@example.test",
            PasswordHash = "test-hash",
            Name = "Nombre visible",
            CurrentMode = "client",
            HasProvider = false,
            Status = "active",
            EmailVerifiedAt = DateTime.UtcNow,
            TermsAcceptedAt = DateTime.UtcNow,
            TermsVersion = "test",
            PhotoUrl = "https://media.example.test/avatar.webp"
        });
        var store = new MongoProfileShareStore(collections, db);
        Assert.Null(await store.ReadAsync(owner, default));
        var share = await store.EnableAsync(owner, default);
        Assert.NotEqual(owner, share);
        Assert.Equal(share, await store.EnableAsync(owner, default));
        var profile = await store.ReadAsync(share, default);
        Assert.Equal("Nombre visible", profile!.Name);
        Assert.Equal("https://media.example.test/avatar.webp", profile.PhotoUrl);
        var json = System.Text.Json.JsonSerializer.Serialize(profile);
        Assert.DoesNotContain("private@example.test", json);
        Assert.DoesNotContain("Password", json);
        await collections.Accounts.UpdateOneAsync(a => a.Id == owner,
            Builders<Account>.Update.Set(a => a.Status, "pendingDeletion"));
        Assert.Null(await store.ReadAsync(share, default));
        await collections.Accounts.UpdateOneAsync(a => a.Id == owner, Builders<Account>.Update.Set(a => a.Status, "active"));
        await store.RevokeAsync(owner, default);
        Assert.Null(await store.ReadAsync(share, default));
        Assert.NotEqual(share, await store.EnableAsync(owner, default));
    }
}

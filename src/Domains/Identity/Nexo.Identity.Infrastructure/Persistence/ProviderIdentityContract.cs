using MongoDB.Driver;
using Nexo.Identity.Contracts;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Infrastructure.Persistence;

public sealed class ProviderIdentityContract(IdentityCollections collections) : IProviderIdentityContract, IPhoneDisclosureContract
{
    public async Task<IReadOnlyDictionary<Guid, ProviderIdentityState>> ReadManyAsync(Guid[] accountIds, CancellationToken ct)
    {
        if (accountIds.Length > 1000) throw new ArgumentOutOfRangeException(nameof(accountIds));
        var values = await collections.Accounts.Find(Builders<Account>.Filter.In(a => a.Id, accountIds.Distinct()))
            .Project(a => new { a.Id, a.Status, a.EmailVerifiedAt, a.Phone, a.PhotoUrl }).ToListAsync(ct);
        // No contact value crosses the Identity contract.
        return values.ToDictionary(a => a.Id, a => new ProviderIdentityState(a.Status == AccountStatus.Active, a.EmailVerifiedAt != default,
            a.Phone is { Length: 13 } && a.Phone.StartsWith("+52", StringComparison.Ordinal) && a.Phone[3..].All(char.IsAsciiDigit), a.PhotoUrl, a.Status == AccountStatus.Deleted));
    }
    public async Task<ProviderIdentityState?> ReadAsync(Guid id, CancellationToken ct)
    {
        var value = await collections.Accounts.Find(a => a.Id == id)
            .Project(a => new { a.Status, a.EmailVerifiedAt, a.Phone, a.PhotoUrl }).FirstOrDefaultAsync(ct);
        return value == null ? null : new(value.Status == AccountStatus.Active, value.EmailVerifiedAt != default,
            value.Phone is { Length: 13 } && value.Phone.StartsWith("+52", StringComparison.Ordinal) && value.Phone[3..].All(char.IsAsciiDigit), value.PhotoUrl, value.Status == AccountStatus.Deleted);
    }
    public async Task<bool> EnableProviderAsync(Guid id, CancellationToken ct)
    {
        var active = Builders<Account>.Filter.Eq(a => a.Id, id) & Builders<Account>.Filter.Eq(a => a.Status, AccountStatus.Active);
        await collections.Accounts.UpdateOneAsync(active & Builders<Account>.Filter.Ne(a => a.HasProvider, true),
            Builders<Account>.Update.Set(a => a.HasProvider, true).Set(a => a.UpdatedAt, DateTime.UtcNow).Inc(a => a.Revision, 1), cancellationToken: ct);
        return await collections.Accounts.Find(active & Builders<Account>.Filter.Eq(a => a.HasProvider, true)).AnyAsync(ct);
    }

    public async Task<string?> GetPhoneAsync(Guid accountId, string reason, CancellationToken ct)
    {
        if (accountId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Length > 80)
            throw new ArgumentException("A valid account and disclosure reason are required.");
        var account = await collections.Accounts.Find(a => a.Id == accountId && a.Status == AccountStatus.Active && a.EmailVerifiedAt != default)
            .Project(a => a.Phone).FirstOrDefaultAsync(ct);
        return account is { Length: 13 } && account.StartsWith("+52", StringComparison.Ordinal) && account[3..].All(char.IsAsciiDigit)
            ? account : null;
    }
}

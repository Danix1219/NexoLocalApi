using MongoDB.Driver;
using Nexo.Identity.Contracts;
using Nexo.Identity.Domain.Accounts;

namespace Nexo.Identity.Infrastructure.Persistence;

public sealed class RequestClientContract(IdentityCollections collections) : IRequestClientContract
{
    public async Task<IReadOnlyDictionary<Guid, RequestClientIdentity>> ReadAsync(Guid[] ids, CancellationToken ct)
    {
        if (ids.Length > 1000) throw new ArgumentOutOfRangeException(nameof(ids));
        var rows = await collections.Accounts.Find(Builders<Account>.Filter.In(a => a.Id, ids.Distinct()))
            .Project(a => new { a.Id, a.Status, a.Name, a.PhotoUrl }).ToListAsync(ct);
        return rows.ToDictionary(a => a.Id, a => new RequestClientIdentity(a.Status == AccountStatus.Active,
            a.Status == AccountStatus.Active ? DisplayName(a.Name) : "Cuenta no disponible", a.Status == AccountStatus.Active ? a.PhotoUrl : null));
    }
    public static string DisplayName(string name) => name.Trim();
}

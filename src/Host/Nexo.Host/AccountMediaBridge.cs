using Nexo.Common.Results;
using Nexo.Common.Errors;
using Nexo.Discovery.Contracts;
using Nexo.Identity.Application.Ports;
using Nexo.Identity.Infrastructure.Safety;
namespace Nexo.Host;
public sealed class AccountMediaBridge(AccountProfileSafety names,IMediaContract media) : IAccountProfileSafety
{
    public Task<Result<bool>> CheckNameAsync(string name,CancellationToken ct)=>names.CheckNameAsync(name,ct);
    public async Task<Result<string>> ResolvePhotoAsync(Guid accountId,Guid uploadId,CancellationToken ct)
    {
        var url=await media.AvatarAsync(accountId,uploadId,ct);
        return url==null?await names.ResolvePhotoAsync(accountId,uploadId,ct):Result<string>.Success(url);
    }
}

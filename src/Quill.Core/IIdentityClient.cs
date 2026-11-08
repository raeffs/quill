using Quill.Core.Models;

namespace Quill.Core;

public interface IIdentityClient
{
    Task<CurrentUser> GetCurrentUserAsync();

    Task<IReadOnlyList<IdentityMatch>> SearchAsync(
        IdentitySearchFilter filter, string value, CancellationToken cancellationToken);
}

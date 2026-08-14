using Quill.Core.Models;

namespace Quill.Core;

public interface IIdentityClient
{
    Task<CurrentUser> GetCurrentUserAsync();
}

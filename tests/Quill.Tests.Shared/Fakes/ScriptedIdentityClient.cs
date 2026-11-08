using Quill.Core;
using Quill.Core.Models;

namespace Quill.Tests.Shared.Fakes;

public sealed class ScriptedIdentityClient : IIdentityClient
{
#pragma warning disable CA1002, MA0016 // Tests assert on the recorded calls as a list
    public List<string> Calls { get; } = [];
#pragma warning restore CA1002, MA0016

    public Func<CurrentUser> GetCurrentUser { get; set; } = () => new CurrentUser("user-1", "Jane Doe");

    public Func<IdentitySearchFilter, string, IReadOnlyList<IdentityMatch>> Search { get; set; } = (_, _) => [];

    public Task<CurrentUser> GetCurrentUserAsync()
    {
        Calls.Add("GetCurrentUser");
        return Task.FromResult(GetCurrentUser());
    }

    public Task<IReadOnlyList<IdentityMatch>> SearchAsync(
        IdentitySearchFilter filter, string value, CancellationToken cancellationToken)
    {
        Calls.Add($"Search({filter},{value})");
        return Task.FromResult(Search(filter, value));
    }
}

using Quill.Core;

namespace Quill.Tests.Shared.Fakes;

public static class TestPersonCache
{
    public static PersonCache Create(
        IPersonStore? store = null,
        TimeProvider? clock = null,
        string serverUrl = "https://devops.example.com",
        string collection = "COL",
        string pat = "test-pat") =>
        new(
            store ?? new InMemoryPersonStore(),
            PersonCacheScope.For(serverUrl, collection, pat),
            clock ?? new FixedClock());
}

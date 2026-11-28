using Shouldly;

namespace Quill.Core.Tests;

public class PersonCachePathTests
{
    [Theory]
    [InlineData("/x/cache", "/x/cache/quill/people.json")]
    [InlineData(null, "/home/jane/.cache/quill/people.json")]
    [InlineData("", "/home/jane/.cache/quill/people.json")]
    [InlineData("relative", "/home/jane/.cache/quill/people.json")]
    public void Resolve_Unix_UsesTheCacheHomeOrTheHome(string? xdgCacheHome, string expected) =>
        PersonCachePath.Resolve(false, xdgCacheHome, "/home/jane", string.Empty).ShouldBe(expected);

    [Fact]
    public void Resolve_UnixWithoutAHome_ReturnsNull() =>
        PersonCachePath.Resolve(false, null, string.Empty, string.Empty).ShouldBeNull();

    [Fact]
    public void Resolve_Windows_UsesLocalAppData() =>
        PersonCachePath.Resolve(true, "/x/cache", "/home/jane", "L").ShouldBe(Path.Combine("L", "quill", "people.json"));

    [Fact]
    public void Resolve_WindowsWithoutLocalAppData_ReturnsNull() =>
        PersonCachePath.Resolve(true, null, "/home/jane", string.Empty).ShouldBeNull();
}

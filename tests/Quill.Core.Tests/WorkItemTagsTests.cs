using Shouldly;

namespace Quill.Core.Tests;

public class WorkItemTagsTests
{
    [Fact]
    public void Normalize_TrimsAndDropsExactDuplicates()
    {
        WorkItemTags.Normalize([" a ", "a", "b"]).ShouldBe(["a", "b"]);
    }

    [Fact]
    public void Normalize_KeepsCaseVariants()
    {
        WorkItemTags.Normalize(["Foo", "foo"]).ShouldBe(["Foo", "foo"]);
    }

    [Theory]
    [InlineData("a;b")]
    [InlineData("a,b")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Normalize_InvalidTag_Throws(string? tag)
    {
        Should.Throw<InvalidOperationException>(() => WorkItemTags.Normalize(["ok", tag]));
    }

    [Fact]
    public void Join_UsesSemicolonSpace()
    {
        WorkItemTags.Join(["a", "b"]).ShouldBe("a; b");
        WorkItemTags.Join([]).ShouldBe(string.Empty);
    }

    [Fact]
    public void Split_TrimsAndDropsEmpties()
    {
        WorkItemTags.Split("a; b;c; ").ShouldBe(["a", "b", "c"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Split_NoValue_ReturnsEmpty(string? value)
    {
        WorkItemTags.Split(value).ShouldBeEmpty();
    }
}

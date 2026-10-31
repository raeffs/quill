using Quill.Core.Markdown;
using Shouldly;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Quill.Core.Tests;

public class FrontmatterListKeysTests
{
    private static readonly IDeserializer Deserializer = FrontmatterParser.BuildDeserializer(new ListKeysYamlContext());

    private static readonly ISerializer Serializer = FrontmatterParser.BuildSerializer(new ListKeysYamlContext());

    [Fact]
    public void Parse_AbsentKeys_AreNull()
    {
        var parsed = Parse("id: 1");

        parsed.Tags.ShouldBeNull();
        parsed.Predecessors.ShouldBeNull();
    }

    [Fact]
    public void Parse_EmptyLists_AreEmptyNotNull()
    {
        var parsed = Parse("id: 1\ntags: []\npredecessors: []");

        parsed.Tags.ShouldNotBeNull();
        parsed.Tags.ShouldBeEmpty();
        parsed.Predecessors.ShouldNotBeNull();
        parsed.Predecessors.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_FilledLists_CarryValues()
    {
        var parsed = Parse("id: 1\ntags:\n  - a\n  - b\npredecessors:\n  - 3\n  - 4");

        parsed.Tags.ShouldBe(["a", "b"]);
        parsed.Predecessors.ShouldBe([3, 4]);
    }

    [Fact]
    public void Parse_FlowAndBlockStyle_Agree()
    {
        var parsed = Parse("id: 1\ntags: [a, b]\npredecessors: [3, 4]");

        parsed.Tags.ShouldBe(["a", "b"]);
        parsed.Predecessors.ShouldBe([3, 4]);
    }

    [Fact]
    public void Parse_KeyWithoutValue_IsNull()
    {
        var parsed = Parse("id: 1\ntags:\npredecessors:");

        parsed.Tags.ShouldBeNull();
        parsed.Predecessors.ShouldBeNull();
    }

    [Fact]
    public void Parse_ScalarForList_Throws()
    {
        Should.Throw<YamlException>(() => Parse("id: 1\ntags: a"));
    }

    [Fact]
    public void Parse_NonIntegerInIntList_Throws()
    {
        Should.Throw<YamlException>(() => Parse("id: 1\npredecessors: [x]"));
    }

    [Fact]
    public void Write_EmptyList_EmitsKeyWithEmptyList()
    {
        var yaml = Serializer.Serialize(new ListKeysFrontmatter { Id = 1, Tags = [], Predecessors = [] });

        yaml.ShouldContain("tags: []");
        yaml.ShouldContain("predecessors: []");
    }

    [Fact]
    public void Write_NullList_OmitsKey()
    {
        var yaml = Serializer.Serialize(new ListKeysFrontmatter { Id = 1 });

        yaml.ShouldNotContain("tags");
        yaml.ShouldNotContain("predecessors");
    }

    [Fact]
    public void WriteThenParse_FilledLists_RoundTrip()
    {
        var yaml = Serializer.Serialize(new ListKeysFrontmatter { Id = 1, Tags = ["a", "b"], Predecessors = [3, 4] });

        var parsed = Parse(yaml);

        parsed.Tags.ShouldBe(["a", "b"]);
        parsed.Predecessors.ShouldBe([3, 4]);
    }

    private static ListKeysFrontmatter Parse(string yaml) => Deserializer.Deserialize<ListKeysFrontmatter>(yaml);
}

internal sealed class ListKeysFrontmatter
{
    public int? Id { get; set; }

#pragma warning disable CA1002, CA2227, MA0016 // The static deserializer needs a settable List<T>; see FrontmatterYamlContext
    public List<string>? Tags { get; set; }

    public List<int>? Predecessors { get; set; }
#pragma warning restore CA1002, CA2227, MA0016
}

[YamlStaticContext]
[YamlSerializable(typeof(ListKeysFrontmatter))]
#pragma warning disable CA1515 // The generator emits its half of the partial context as public
public partial class ListKeysYamlContext : StaticContext
#pragma warning restore CA1515
{
}

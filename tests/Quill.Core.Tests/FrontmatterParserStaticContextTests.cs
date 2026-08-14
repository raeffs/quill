using Quill.Core.Markdown;
using Shouldly;

namespace Quill.Core.Tests;

public class FrontmatterParserStaticContextTests
{
    private const string FileWithExtraKeys =
        "---\n" +
        "id: 42\n" +
        "type: Bug\n" +
        "title: Hello\n" +
        "state: Active\n" +
        "parentId: 7\n" +
        "tags:\n" +
        "  - a\n" +
        "  - b\n" +
        "meta:\n" +
        "  k: v\n" +
        "---\n" +
        "\n" +
        "Body\n";

    [Fact]
    public void Parse_IgnoresUnmodelledKeys()
    {
        var parsed = FrontmatterParser.Parse(FileWithExtraKeys);

        parsed.Id.ShouldBe(42);
        parsed.Type.ShouldBe("Bug");
        parsed.Title.ShouldBe("Hello");
        parsed.State.ShouldBe("Active");
        parsed.ParentId.ShouldBe(7);
        parsed.Body.ShouldBe("Body\n");
    }

    [Fact]
    public void Parse_AcceptsQuotedAndColonBearingTitles()
    {
        var content = "---\nid: 1\ntitle: \"Fix: the thing #2\"\n---\n\nBody\n";

        FrontmatterParser.Parse(content).Title.ShouldBe("Fix: the thing #2");
    }

    [Fact]
    public void WriteThenParse_RoundTrips()
    {
        var yaml = FrontmatterParser.Write(9, "Task", "Round trip", "New", "Body", parentId: 3);

        var parsed = FrontmatterParser.Parse(yaml);

        parsed.Id.ShouldBe(9);
        parsed.Type.ShouldBe("Task");
        parsed.Title.ShouldBe("Round trip");
        parsed.State.ShouldBe("New");
        parsed.ParentId.ShouldBe(3);
    }

    [Fact]
    public void Write_OmitsParentIdWhenNull()
    {
        var yaml = FrontmatterParser.Write(9, "Task", "No parent", "New", "Body");

        yaml.ShouldNotContain("parentId");
    }
}

using Quill.Core.Markdown;
using Shouldly;

namespace Quill.Core.Tests.Markdown;

public class FrontmatterParserTests
{
    [Fact]
    public void Parse_ValidFileContent_ReturnsFrontmatterAndBody()
    {
        var content = """
            ---
            id: 12345
            type: Product Backlog Item
            title: Implement user login
            state: Active
            ---

            This is the description.

            ## Details

            Some details here.
            """;

        var result = FrontmatterParser.Parse(content);

        result.Id.ShouldBe(12345);
        result.Type.ShouldBe("Product Backlog Item");
        result.Title.ShouldBe("Implement user login");
        result.State.ShouldBe("Active");
        result.Body.Trim().ShouldBe("""
            This is the description.

            ## Details

            Some details here.
            """.Trim());
    }

    [Fact]
    public void Parse_MissingId_ThrowsInvalidOperationException()
    {
        var content = """
            ---
            title: No ID here
            ---

            Body text.
            """;

        var act = () => FrontmatterParser.Parse(content);

        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("id");
    }

    [Fact]
    public void Parse_NoFrontmatter_ThrowsInvalidOperationException()
    {
        var content = "Just a plain markdown file.";

        var act = () => FrontmatterParser.Parse(content);

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Write_ProducesValidMarkdown()
    {
        var output = FrontmatterParser.Write(
            id: 12345,
            type: "Product Backlog Item",
            title: "Implement user login",
            state: "Active",
            body: "This is the description.");

        var parsed = FrontmatterParser.Parse(output);

        parsed.Id.ShouldBe(12345);
        parsed.Type.ShouldBe("Product Backlog Item");
        parsed.Title.ShouldBe("Implement user login");
        parsed.State.ShouldBe("Active");
        parsed.Body.Trim().ShouldBe("This is the description.");
    }

    [Fact]
    public void Parse_MissingTitle_ThrowsInvalidOperationException()
    {
        var content = """
            ---
            id: 12345
            ---

            Body text.
            """;

        var act = () => FrontmatterParser.Parse(content);

        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("title");
    }

    [Fact]
    public void Parse_MissingClosingDelimiter_ThrowsInvalidOperationException()
    {
        var content = """
            ---
            id: 12345
            title: Test
            """;

        var act = () => FrontmatterParser.Parse(content);

        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("closing delimiter");
    }

    [Fact]
    public void Parse_EmptyBody_ReturnsEmptyBody()
    {
        var content = """
            ---
            id: 12345
            title: Test Item
            ---
            """;

        var result = FrontmatterParser.Parse(content);

        result.Id.ShouldBe(12345);
        result.Title.ShouldBe("Test Item");
        result.Body.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_SpecialCharactersInTitle_PreservesTitle()
    {
        var content = """
            ---
            id: 12345
            title: "Fix: handle <angle> & \"quotes\""
            ---

            Body.
            """;

        var result = FrontmatterParser.Parse(content);

        result.Title.ShouldBe("Fix: handle <angle> & \"quotes\"");
    }

    [Fact]
    public void Parse_ExtraFrontmatterFields_AreIgnored()
    {
        var content = """
            ---
            id: 12345
            title: Test
            customField: value
            anotherField: 42
            ---

            Body.
            """;

        var result = FrontmatterParser.Parse(content);

        result.Id.ShouldBe(12345);
        result.Title.ShouldBe("Test");
    }

    [Fact]
    public void Parse_WithParentId_ReturnsParentId()
    {
        var content = """
            ---
            id: 0
            type: Bug
            title: New bug
            state: New
            parentId: 100
            ---

            Body.
            """;

        var result = FrontmatterParser.Parse(content);

        result.ParentId.ShouldBe(100);
    }

    [Fact]
    public void Parse_WithoutParentId_ReturnsNullParentId()
    {
        var content = """
            ---
            id: 200
            type: Bug
            title: No parent
            state: Active
            ---

            Body.
            """;

        var result = FrontmatterParser.Parse(content);

        result.ParentId.ShouldBeNull();
    }

    [Fact]
    public void Write_WithParentId_IncludesParentIdInFrontmatter()
    {
        var output = FrontmatterParser.Write(
            id: 200,
            type: "Task",
            title: "Child task",
            state: "New",
            body: "Body.",
            parentId: 100);

        var parsed = FrontmatterParser.Parse(output);

        parsed.ParentId.ShouldBe(100);
    }

    [Fact]
    public void Write_WithoutParentId_OmitsParentIdFromFrontmatter()
    {
        var output = FrontmatterParser.Write(
            id: 200,
            type: "Bug",
            title: "No parent",
            state: "Active",
            body: "Body.");

        output.ShouldNotContain("parentId");
    }

    [Fact]
    public void Write_RoundTripsWithParse()
    {
        var output = FrontmatterParser.Write(
            id: 99999,
            type: "Bug",
            title: "Edge case: special chars & symbols",
            state: "New",
            body: "Line 1\n\nLine 2");

        var parsed = FrontmatterParser.Parse(output);

        parsed.Id.ShouldBe(99999);
        parsed.Type.ShouldBe("Bug");
        parsed.Title.ShouldBe("Edge case: special chars & symbols");
        parsed.State.ShouldBe("New");
        parsed.Body.Trim().ShouldBe("Line 1\n\nLine 2");
    }
}

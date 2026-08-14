using Quill.Core;
using Shouldly;

namespace Quill.Core.Tests;

public class WiqlBuilderTests
{
    [Fact]
    public void Build_WithNoInputs_Throws()
    {
        Should.Throw<InvalidOperationException>(() =>
            WiqlBuilder.Build(null, null, [], [], 50));
    }

    [Fact]
    public void Build_WithWhitespaceOnlyInputs_Throws()
    {
        Should.Throw<InvalidOperationException>(() =>
            WiqlBuilder.Build("   ", " ", [], [], 50));
    }

    [Fact]
    public void Build_WithZeroOrNegativeLimit_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            WiqlBuilder.Build("x", null, [], [], 0));
    }

    [Fact]
    public void Build_WithQueryOnly_UsesContainsWordsOnTitle()
    {
        var (wiql, top) = WiqlBuilder.Build("login validation", null, [], [], 50);

        wiql.ShouldContain("[System.Title] CONTAINS WORDS 'login validation'");
        wiql.ShouldContain("ORDER BY [System.ChangedDate] DESC");
        top.ShouldBe(50);
    }

    [Fact]
    public void Build_WithAssigneeAtMe_ExpandsToAtMeMacroUnquoted()
    {
        var (wiql, _) = WiqlBuilder.Build(null, "@me", [], [], 50);

        wiql.ShouldContain("[System.AssignedTo] = @Me");
        wiql.ShouldNotContain("'@Me'");
    }

    [Fact]
    public void Build_WithAssigneeAtMeMixedCase_ExpandsToAtMeMacro()
    {
        var (wiql, _) = WiqlBuilder.Build(null, "@ME", [], [], 50);

        wiql.ShouldContain("[System.AssignedTo] = @Me");
    }

    [Fact]
    public void Build_WithAssigneeDisplayName_QuotesAsLiteral()
    {
        var (wiql, _) = WiqlBuilder.Build(null, "Jane Doe", [], [], 50);

        wiql.ShouldContain("[System.AssignedTo] = 'Jane Doe'");
    }

    [Fact]
    public void Build_WithAssigneeContainingSingleQuote_DoublesIt()
    {
        var (wiql, _) = WiqlBuilder.Build(null, "O'Brien", [], [], 50);

        wiql.ShouldContain("[System.AssignedTo] = 'O''Brien'");
    }

    [Fact]
    public void Build_WithQueryContainingSingleQuote_DoublesIt()
    {
        var (wiql, _) = WiqlBuilder.Build("can't do", null, [], [], 50);

        wiql.ShouldContain("[System.Title] CONTAINS WORDS 'can''t do'");
    }

    [Fact]
    public void Build_WithSingleState_UsesInClauseWithOneLiteral()
    {
        var (wiql, _) = WiqlBuilder.Build(null, null, ["Active"], [], 50);

        wiql.ShouldContain("[System.State] IN ('Active')");
    }

    [Fact]
    public void Build_WithMultipleStates_JoinsLiteralsInInClause()
    {
        var (wiql, _) = WiqlBuilder.Build(null, null, ["Active", "New"], [], 50);

        wiql.ShouldContain("[System.State] IN ('Active', 'New')");
    }

    [Fact]
    public void Build_WithTypes_UsesWorkItemTypeInClause()
    {
        var (wiql, _) = WiqlBuilder.Build(null, null, [], ["Bug", "Product Backlog Item"], 50);

        wiql.ShouldContain("[System.WorkItemType] IN ('Bug', 'Product Backlog Item')");
    }

    [Fact]
    public void Build_CombinesAllClausesWithAnd()
    {
        var (wiql, top) = WiqlBuilder.Build("login", "@me", ["Active"], ["Bug"], 25);

        wiql.ShouldBe(
            "SELECT [System.Id] FROM WorkItems WHERE " +
            "[System.Title] CONTAINS WORDS 'login' AND " +
            "[System.AssignedTo] = @Me AND " +
            "[System.State] IN ('Active') AND " +
            "[System.WorkItemType] IN ('Bug') " +
            "ORDER BY [System.ChangedDate] DESC");
        top.ShouldBe(25);
    }
}

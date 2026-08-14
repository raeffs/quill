using System.Text.Json;
using Quill.Core;
using Quill.Core.Models;
using Shouldly;

namespace Quill.Core.Tests.Commands;

public class ViewCommandLogicTests
{
    [Fact]
    public void BuildViewResult_PopulatedFields_MapsAllKeys()
    {
        var workItem = new WorkItem
        {
            Id = 12345,
            Type = "Product Backlog Item",
            Title = "The title",
            State = "Active",
            AssignedToId = "44892788-c082-4795-a323-8cc6daaaaba2",
            AssignedToDisplayName = "Jane Doe",
            Description = "<p>ignored here</p>",
            ParentId = 67890,
            Relations =
            [
                new WorkItemRelation { RelationType = "System.LinkTypes.Related", TargetId = 201 },
                new WorkItemRelation { RelationType = "System.LinkTypes.Related", TargetId = 202 },
            ],
            ChildIds = [],
        };

        var result = ViewResultBuilder.Build(workItem, "<markdown body>", children: null);

        result.Id.ShouldBe(12345);
        result.Type.ShouldBe("Product Backlog Item");
        result.Title.ShouldBe("The title");
        result.State.ShouldBe("Active");
        result.AssignedTo.ShouldBe("Jane Doe");
        result.ParentId.ShouldBe(67890);
        result.Description.ShouldBe("<markdown body>");
        result.RelatedIds.ShouldBe([201, 202]);
        result.Children.ShouldBeNull();
    }

    [Fact]
    public void BuildViewResult_EmptyFields_EmitsNullsAndEmptyArrays()
    {
        var workItem = new WorkItem
        {
            Id = 5,
            Type = "Task",
            Title = "t",
            State = "New",
            AssignedToId = string.Empty,
            AssignedToDisplayName = string.Empty,
            Description = string.Empty,
            ParentId = null,
            Relations = [],
            ChildIds = [],
        };

        var result = ViewResultBuilder.Build(workItem, string.Empty, children: null);

        result.AssignedTo.ShouldBeNull();
        result.ParentId.ShouldBeNull();
        result.Description.ShouldBe(string.Empty);
        result.RelatedIds.ShouldBeEmpty();
    }

    [Fact]
    public void BuildViewResult_Serialized_HasEightKeysAndNullsEmitted()
    {
        var workItem = new WorkItem
        {
            Id = 5,
            Type = "Task",
            Title = "t",
            State = "New",
            AssignedToId = string.Empty,
            AssignedToDisplayName = string.Empty,
            Description = string.Empty,
            ParentId = null,
            Relations = [],
            ChildIds = [],
        };

        var result = ViewResultBuilder.Build(workItem, string.Empty, children: null);
        var json = JsonSerializer.Serialize(result);

        using var doc = JsonDocument.Parse(json);
        var names = doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        names.ShouldBe(["id", "type", "title", "state", "assignedTo", "parentId", "description", "relatedIds"]);
        doc.RootElement.GetProperty("assignedTo").ValueKind.ShouldBe(JsonValueKind.Null);
        doc.RootElement.GetProperty("parentId").ValueKind.ShouldBe(JsonValueKind.Null);
        doc.RootElement.GetProperty("relatedIds").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public void BuildViewResult_WithChildren_AddsChildrenKey()
    {
        var workItem = new WorkItem
        {
            Id = 1,
            Type = "Feature",
            Title = "f",
            State = "Active",
            AssignedToId = string.Empty,
            Description = string.Empty,
            ChildIds = [10, 11],
        };

        var children = new List<ChildItem>
        {
            new() { Id = 10, Title = "c1", State = "New" },
            new() { Id = 11, Title = "c2", State = "Active" },
        };

        var result = ViewResultBuilder.Build(workItem, string.Empty, children);

        result.Children!.Count.ShouldBe(2);
        result.Children[0].Id.ShouldBe(10);
        result.Children[0].Title.ShouldBe("c1");
        result.Children[0].State.ShouldBe("New");
    }

    [Fact]
    public void BuildViewResult_WithChildrenEmpty_EmitsEmptyArray()
    {
        var workItem = new WorkItem
        {
            Id = 1,
            Type = "Feature",
            Title = "f",
            State = "Active",
            AssignedToId = string.Empty,
            Description = string.Empty,
            ChildIds = [],
        };

        var result = ViewResultBuilder.Build(workItem, string.Empty, children: []);
        var json = JsonSerializer.Serialize(result);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("children").GetArrayLength().ShouldBe(0);
    }
}

using System.Text.Json.Serialization;

namespace Quill.Core.Models;

public class PushResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("updatedFields")]
    public required IReadOnlyList<string> UpdatedFields { get; init; }

    [JsonPropertyName("relationsAdded")]
    public required IReadOnlyList<int> RelationsAdded { get; init; }
}

public class PullResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("file")]
    public required string File { get; init; }
}

public class CreateResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }
}

public class ViewResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; init; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("relatedIds")]
    public required IReadOnlyList<int> RelatedIds { get; init; }

    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ChildItem>? Children { get; init; }
}

public class ChildItem
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }
}

public class SearchResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; init; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; init; }
}

public class CommentResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("modifiedDate")]
    public string? ModifiedDate { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

public class ErrorResult
{
    [JsonPropertyName("error")]
    public required string Error { get; init; }

    [JsonPropertyName("code")]
    public required int Code { get; init; }
}

public class PullRequestResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("author")]
    public required string Author { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("isDraft")]
    public required bool IsDraft { get; init; }

    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("sourceBranch")]
    public required string SourceBranch { get; init; }

    [JsonPropertyName("targetBranch")]
    public required string TargetBranch { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("closedDate")]
    public string? ClosedDate { get; init; }

    [JsonPropertyName("mergeStatus")]
    public string? MergeStatus { get; init; }

    [JsonPropertyName("labels")]
    public required IReadOnlyList<string> Labels { get; init; }

    [JsonPropertyName("votes")]
    public required PullRequestVoteCountsResult Votes { get; init; }

    [JsonPropertyName("myVote")]
    public string? MyVote { get; init; }

    [JsonPropertyName("myIsRequired")]
    public bool? MyIsRequired { get; init; }
}

public class PullRequestVoteCountsResult
{
    [JsonPropertyName("approved")]
    public required int Approved { get; init; }

    [JsonPropertyName("waitingForAuthor")]
    public required int WaitingForAuthor { get; init; }

    [JsonPropertyName("rejected")]
    public required int Rejected { get; init; }

    [JsonPropertyName("noVote")]
    public required int NoVote { get; init; }
}

public class PullRequestReviewerResult
{
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("vote")]
    public required string Vote { get; init; }

    [JsonPropertyName("isRequired")]
    public required bool IsRequired { get; init; }

    [JsonPropertyName("isContainer")]
    public required bool IsContainer { get; init; }
}

public class PullRequestThreadResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("filePath")]
    public string? FilePath { get; init; }

    [JsonPropertyName("side")]
    public string? Side { get; init; }

    [JsonPropertyName("startLine")]
    public int? StartLine { get; init; }

    [JsonPropertyName("endLine")]
    public int? EndLine { get; init; }

    [JsonPropertyName("positionState")]
    public string? PositionState { get; init; }

    [JsonPropertyName("origFilePath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OrigFilePath { get; init; }

    [JsonPropertyName("origStartLine")]
    public int? OrigStartLine { get; init; }

    [JsonPropertyName("origEndLine")]
    public int? OrigEndLine { get; init; }

    [JsonPropertyName("origStartColumn")]
    public int? OrigStartColumn { get; init; }

    [JsonPropertyName("origEndColumn")]
    public int? OrigEndColumn { get; init; }

    [JsonPropertyName("publishedDate")]
    public required string PublishedDate { get; init; }

    [JsonPropertyName("lastUpdatedDate")]
    public required string LastUpdatedDate { get; init; }

    [JsonPropertyName("comments")]
    public required IReadOnlyList<PullRequestCommentResult> Comments { get; init; }
}

public class PullRequestCommentResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("modifiedDate")]
    public string? ModifiedDate { get; init; }

    [JsonPropertyName("lastUpdatedDate")]
    public required string LastUpdatedDate { get; init; }

    [JsonPropertyName("usersLiked")]
    public required IReadOnlyList<string> UsersLiked { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

public class PullRequestRevisionResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("sourceCommit")]
    public string? SourceCommit { get; init; }

    [JsonPropertyName("targetCommit")]
    public string? TargetCommit { get; init; }

    [JsonPropertyName("commonCommit")]
    public string? CommonCommit { get; init; }
}

public class PullRequestViewResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("author")]
    public required string Author { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("isDraft")]
    public required bool IsDraft { get; init; }

    [JsonPropertyName("repo")]
    public required string Repo { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("sourceBranch")]
    public required string SourceBranch { get; init; }

    [JsonPropertyName("targetBranch")]
    public required string TargetBranch { get; init; }

    [JsonPropertyName("createdDate")]
    public required string CreatedDate { get; init; }

    [JsonPropertyName("closedDate")]
    public string? ClosedDate { get; init; }

    [JsonPropertyName("mergeStatus")]
    public string? MergeStatus { get; init; }

    [JsonPropertyName("lastMergeSourceCommit")]
    public string? LastMergeSourceCommit { get; init; }

    [JsonPropertyName("lastMergeTargetCommit")]
    public string? LastMergeTargetCommit { get; init; }

    [JsonPropertyName("labels")]
    public required IReadOnlyList<string> Labels { get; init; }

    [JsonPropertyName("votes")]
    public required PullRequestVoteCountsResult Votes { get; init; }

    [JsonPropertyName("reviewers")]
    public required IReadOnlyList<PullRequestReviewerResult> Reviewers { get; init; }

    [JsonPropertyName("myVote")]
    public string? MyVote { get; init; }

    [JsonPropertyName("myIsRequired")]
    public bool? MyIsRequired { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("workItems")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PullRequestLinkedWorkItemResult>? WorkItems { get; init; }

    [JsonPropertyName("threads")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PullRequestThreadResult>? Threads { get; init; }
}

public class PullRequestLinkedWorkItemResult
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? State { get; init; }

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    [JsonPropertyName("assignedTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AssignedTo { get; init; }

    [JsonPropertyName("parentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ParentId { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }
}

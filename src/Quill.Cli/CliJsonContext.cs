using System.Text.Json.Serialization;
using Quill.Core.Models;

namespace Quill.Cli;

[JsonSerializable(typeof(ErrorResult))]
[JsonSerializable(typeof(PushResult))]
[JsonSerializable(typeof(PullResult))]
[JsonSerializable(typeof(ViewResult))]
[JsonSerializable(typeof(CreateResult))]
[JsonSerializable(typeof(List<ChildItem>))]
[JsonSerializable(typeof(List<SearchResult>))]
[JsonSerializable(typeof(IReadOnlyList<CommentResult>), TypeInfoPropertyName = "ListCommentResult")]
[JsonSerializable(typeof(IReadOnlyList<PullRequestResult>), TypeInfoPropertyName = "ListPullRequestResult")]
[JsonSerializable(typeof(IReadOnlyList<PullRequestRevisionResult>), TypeInfoPropertyName = "ListPullRequestRevisionResult")]
[JsonSerializable(typeof(IReadOnlyList<PullRequestThreadResult>), TypeInfoPropertyName = "ListPullRequestThreadResult")]
[JsonSerializable(typeof(PullRequestViewResult))]
[JsonSerializable(typeof(PullRequestChecksResult))]
[JsonSerializable(typeof(IReadOnlyList<PipelineResult>), TypeInfoPropertyName = "ListPipelineResult")]
[JsonSerializable(typeof(PipelineRunResult))]
[JsonSerializable(typeof(PipelineLogResult))]
[JsonSerializable(typeof(IReadOnlyList<PipelineRunResult>), TypeInfoPropertyName = "ListPipelineRunResult")]
[JsonSerializable(typeof(IReadOnlyList<PipelineTestRunResult>), TypeInfoPropertyName = "ListPipelineTestRunResult")]
[JsonSerializable(typeof(TreeNode))]
internal sealed partial class CliJsonContext : JsonSerializerContext
{
}

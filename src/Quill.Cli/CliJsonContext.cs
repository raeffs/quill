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
[JsonSerializable(typeof(List<CommentResult>))]
[JsonSerializable(typeof(List<PullRequestResult>))]
[JsonSerializable(typeof(List<PullRequestThreadResult>))]
[JsonSerializable(typeof(PullRequestViewResult))]
[JsonSerializable(typeof(TreeNode))]
internal sealed partial class CliJsonContext : JsonSerializerContext
{
}

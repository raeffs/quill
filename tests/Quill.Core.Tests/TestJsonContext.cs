using System.Text.Json.Serialization;
using Quill.Core.Models;

namespace Quill.Core.Tests;

[JsonSerializable(typeof(TreeNode))]
internal sealed partial class TestJsonContext : JsonSerializerContext
{
}

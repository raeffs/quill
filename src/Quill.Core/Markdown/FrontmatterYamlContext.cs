using YamlDotNet.Serialization;

namespace Quill.Core.Markdown;

/// <summary>
/// Source-generated YamlDotNet serialization context.
/// Register only concrete Quill types here. Adding a generic BCL collection —
/// for example [YamlSerializable(typeof(Dictionary&lt;string, object&gt;))], which is
/// what YamlDotNet's own error message suggests — crashes the generator with
/// CS8785 and yields a binary where every serializer call throws.
/// </summary>
[YamlStaticContext]
[YamlSerializable(typeof(Frontmatter))]
public partial class FrontmatterYamlContext : StaticContext
{
}

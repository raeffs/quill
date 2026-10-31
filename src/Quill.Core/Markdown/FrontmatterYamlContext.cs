using YamlDotNet.Serialization;

namespace Quill.Core.Markdown;

/// <summary>
/// Register only concrete Quill types: a generic BCL collection crashes the
/// generator (CS8785). A list key is a nullable List&lt;T&gt; property with no
/// initializer, so an absent key stays apart from <c>[]</c> (ADR 0008).
/// </summary>
[YamlStaticContext]
[YamlSerializable(typeof(Frontmatter))]
public partial class FrontmatterYamlContext : StaticContext
{
}

using YamlDotNet.Serialization;

namespace Quill.Core.Tests;

[YamlStaticContext]
[YamlSerializable(typeof(ListKeysFrontmatter))]
#pragma warning disable CA1515 // The generator emits its half of the partial context as public
public partial class ListKeysYamlContext : StaticContext
#pragma warning restore CA1515
{
}

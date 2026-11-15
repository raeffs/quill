namespace Quill.Core.Tests;

internal sealed class ListKeysFrontmatter
{
    public int? Id { get; set; }

#pragma warning disable CA1002, CA2227, MA0016 // The static deserializer needs a settable List<T>; see FrontmatterYamlContext
    public List<string>? Tags { get; set; }

    public List<int>? Predecessors { get; set; }
#pragma warning restore CA1002, CA2227, MA0016
}

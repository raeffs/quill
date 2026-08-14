namespace Quill.Core.Markdown;

/// <summary>
/// The five frontmatter fields Quill models. Deliberately a typed class, not a
/// dictionary: the YamlDotNet static context cannot serialize generic BCL
/// collections, and the reflective builder that can is not AOT-safe.
/// See docs/adr/0003-typed-frontmatter-for-native-aot.md.
/// </summary>
/// <remarks>
/// <see cref="Id"/> is nullable so an absent id stays distinguishable from
/// <c>id: 0</c>, which is the literal value a file awaiting creation carries.
/// </remarks>
public sealed class Frontmatter
{
    public int? Id { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public string? State { get; set; }

    public int? ParentId { get; set; }
}

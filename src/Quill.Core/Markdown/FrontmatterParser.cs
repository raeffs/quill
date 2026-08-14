using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Quill.Core.Markdown;

public record ParsedMarkdownFile(int Id, string? Type, string Title, string? State, string Body, int? ParentId = null);

public static class FrontmatterParser
{
    private const string Delimiter = "---";

    private static readonly IDeserializer Deserializer =
        new StaticDeserializerBuilder(new FrontmatterYamlContext())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private static readonly ISerializer Serializer =
        new StaticSerializerBuilder(new FrontmatterYamlContext())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

    public static ParsedMarkdownFile Parse(string content)
    {
        var lines = content.Split('\n');
        var trimmedFirst = lines[0].Trim();
        if (!string.Equals(trimmedFirst, Delimiter, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Markdown file must start with YAML frontmatter (---).");
        }

        var endIndex = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.Equals(lines[i].Trim(), Delimiter, StringComparison.Ordinal))
            {
                endIndex = i;
                break;
            }
        }

        if (endIndex == -1)
        {
            throw new InvalidOperationException("Frontmatter closing delimiter (---) not found.");
        }

        var yamlContent = string.Join('\n', lines[1..endIndex]);
        var body = string.Join('\n', lines[(endIndex + 1)..]).TrimStart('\r', '\n');

        var frontmatter = Deserializer.Deserialize<Frontmatter>(yamlContent);

        // Id is nullable precisely so this stays distinguishable from `id: 0`,
        // which is what a file awaiting creation carries.
        var id = frontmatter?.Id
            ?? throw new InvalidOperationException("Frontmatter must contain an 'id' field.");

        var title = frontmatter.Title
            ?? throw new InvalidOperationException("Frontmatter must contain a 'title' field.");

        return new ParsedMarkdownFile(
            id,
            frontmatter.Type,
            title,
            frontmatter.State,
            body,
            ParentId: frontmatter.ParentId);
    }

    public static string Write(int id, string type, string title, string state, string body, int? parentId = null)
    {
        var frontmatter = new Frontmatter
        {
            Id = id,
            Type = type,
            Title = title,
            State = state,
            ParentId = parentId,
        };

        var yaml = Serializer.Serialize(frontmatter).TrimEnd();

        return $"---\n{yaml}\n---\n\n{body}\n";
    }
}

using System.Globalization;
using System.Text;
using ColorCode;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Parsing;

namespace Quill.Core.Markdown;

public static class SyntaxHighlighter
{
    private const string DefaultTextColor = "#c6d0f5";

    private const string OuterDivStyle =
        "color: " + DefaultTextColor + "; background-color: #303446; font-family: Consolas, 'Courier New', monospace; font-size: 14px; line-height: 20px; padding: 12px; border-radius: 4px;";

    private static readonly LanguageParser Parser = CreateParser();

    private static readonly Dictionary<string, ILanguage> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp"] = Languages.CSharp,
        ["cs"] = Languages.CSharp,
        ["typescript"] = Languages.Typescript,
        ["ts"] = Languages.Typescript,
        ["json"] = FindLanguage("json"),
        ["sql"] = Languages.Sql,
    };

    // Catppuccin Frappé palette mapped to ColorCode scope names.
    private static readonly Dictionary<string, string> ScopeColors = new(StringComparer.Ordinal)
    {
        [ScopeName.Keyword] = "#ca9ee6",              // Mauve
        [ScopeName.ControlKeyword] = "#ca9ee6",       // Mauve
        [ScopeName.PseudoKeyword] = "#ca9ee6",        // Mauve
        [ScopeName.PreprocessorKeyword] = "#949cbb",  // Overlay1
        [ScopeName.Comment] = "#737994",              // Overlay0
        [ScopeName.XmlDocComment] = "#737994",        // Overlay0
        [ScopeName.XmlDocTag] = "#737994",            // Overlay0
        [ScopeName.String] = "#a6d189",               // Green
        [ScopeName.StringCSharpVerbatim] = "#a6d189", // Green
        [ScopeName.StringEscape] = "#f2d5cf",         // Rosewater
        [ScopeName.ClassName] = "#e5c890",            // Yellow
        [ScopeName.Type] = "#e5c890",                 // Yellow
        [ScopeName.TypeVariable] = "#e5c890",         // Yellow
        [ScopeName.NameSpace] = "#e5c890",            // Yellow
        [ScopeName.Constructor] = "#e5c890",          // Yellow
        [ScopeName.Predefined] = "#e5c890",           // Yellow
        [ScopeName.Number] = "#fab387",               // Peach
        [ScopeName.Operator] = "#949cbb",             // Overlay1
        [ScopeName.Delimiter] = "#949cbb",            // Overlay1
        [ScopeName.Brackets] = "#949cbb",             // Overlay1
        [ScopeName.PlainText] = DefaultTextColor,
        [ScopeName.JsonKey] = "#8caaee",              // Blue
        [ScopeName.JsonString] = "#a6d189",           // Green
        [ScopeName.JsonNumber] = "#fab387",           // Peach
        [ScopeName.JsonConst] = "#fab387",            // Peach
        [ScopeName.SqlSystemFunction] = "#8caaee",    // Blue
        [ScopeName.Intrinsic] = "#8caaee",            // Blue
    };

    public static string? Highlight(string sourceCode, string language)
    {
        if (!SupportedLanguages.TryGetValue(language, out var lang))
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"<div data-lang=\"{language}\" style=\"{OuterDivStyle}\">");

        var state = new LineState();

        Parser.Parse(sourceCode, lang, (parsedCode, captures) =>
        {
            if (captures.Count == 0)
            {
                SplitIntoLines(parsedCode, null, state);
            }
            else
            {
                var pos = 0;
                foreach (var scope in captures)
                {
                    if (scope.Index > pos)
                    {
                        var gap = parsedCode[pos..scope.Index];
                        SplitIntoLines(gap, null, state);
                    }

                    var text = parsedCode.Substring(scope.Index, scope.Length);
                    SplitIntoLines(text, scope.Name, state);
                    pos = scope.Index + scope.Length;
                }

                if (pos < parsedCode.Length)
                {
                    SplitIntoLines(parsedCode[pos..], null, state);
                }
            }
        });

        foreach (var line in state.Lines)
        {
            sb.Append("<div>");
            if (line.Count == 0)
            {
                sb.Append("<br>");
            }
            else
            {
                foreach (var (text, scopeName) in line)
                {
                    var encoded = EncodeForHtml(text);
                    if (scopeName is not null
                        && ScopeColors.TryGetValue(scopeName, out var color)
                        && !string.Equals(color, DefaultTextColor, StringComparison.Ordinal))
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"<span style=\"color: {color};\">{encoded}</span>");
                    }
                    else
                    {
                        sb.Append(CultureInfo.InvariantCulture, $"<span>{encoded}</span>");
                    }
                }
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private static LanguageParser CreateParser()
    {
        var allLanguages = Languages.All.ToDictionary(l => l.Id, l => l, StringComparer.OrdinalIgnoreCase);
        var repo = new LanguageRepository(allLanguages);
        var compiledLanguages = new Dictionary<string, CompiledLanguage>(StringComparer.OrdinalIgnoreCase);
#pragma warning disable CA2000 // ReaderWriterLockSlim lives for the application lifetime
        var compileLock = new System.Threading.ReaderWriterLockSlim();
#pragma warning restore CA2000
        var compiler = new LanguageCompiler(compiledLanguages, compileLock);
        return new LanguageParser(compiler, repo);
    }

    private static ILanguage FindLanguage(string id)
    {
        return Languages.All.First(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    private static void SplitIntoLines(string text, string? scopeName, LineState state)
    {
        var parts = text.Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                state.CurrentLine = [];
                state.Lines.Add(state.CurrentLine);
            }

            if (parts[i].Length > 0)
            {
                state.CurrentLine.Add((parts[i], scopeName));
            }
        }
    }

    private static string EncodeForHtml(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case ' ':
                    sb.Append("&nbsp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '&':
                    sb.Append("&amp;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private sealed class LineState
    {
        public LineState()
        {
            Lines.Add(CurrentLine);
        }

        public List<List<(string Text, string? ScopeName)>> Lines { get; } = [];
        public List<(string Text, string? ScopeName)> CurrentLine { get; set; } = [];
    }
}

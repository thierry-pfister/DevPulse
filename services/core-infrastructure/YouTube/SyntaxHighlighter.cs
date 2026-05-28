using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace DevPulse.Infrastructure.YouTube;

internal static class SyntaxHighlighter
{
    private static readonly string[] Keywords =
    [
        "abstract", "and", "as", "async", "await", "base", "bool", "break",
        "byte", "case", "catch", "char", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "elif", "else",
        "end", "enum", "event", "exception", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "fun", "function", "goto",
        "if", "implicit", "in", "inherit", "inline", "int", "interface",
        "internal", "is", "lazy", "let", "lock", "long", "match", "member",
        "module", "mutable", "namespace", "new", "not", "null", "object",
        "of", "open", "or", "override", "params", "private", "protected",
        "public", "readonly", "rec", "ref", "return", "sbyte", "sealed",
        "short", "static", "string", "struct", "switch", "then", "this",
        "throw", "true", "try", "type", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "use", "ushort", "using", "val", "var",
        "virtual", "void", "volatile", "when", "while", "with", "yield",
    ];

    private static readonly Regex TokenRegex = new(
        @"(?<comment>//[^\n]*)"                                         +
        @"|(?<string>""[^""\n\\]*(?:\\.[^""\n\\]*)*"")"                +
        $@"|(?<keyword>\b({string.Join("|", Keywords)})\b)"            +
        @"|(?<type>\b[A-Z][a-zA-Z0-9]*\b)"                            +
        @"|(?<number>\b\d+(?:\.\d+)?[mflLuU]?\b)",
        RegexOptions.Compiled);

    internal static string Highlight(string code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        var result = new StringBuilder();
        var pos    = 0;
        foreach (Match m in TokenRegex.Matches(code))
        {
            if (m.Index > pos)
                result.Append(HttpUtility.HtmlEncode(code[pos..m.Index]));
            var color = m.Groups["comment"].Success ? "#8b949e"
                      : m.Groups["string"].Success  ? "#a5d6ff"
                      : m.Groups["keyword"].Success ? "#ff7b72"
                      : m.Groups["type"].Success    ? "#ffa657"
                      :                               "#79c0ff";
            result.Append($"<span style=\"color:{color}\">{HttpUtility.HtmlEncode(m.Value)}</span>");
            pos = m.Index + m.Length;
        }
        if (pos < code.Length)
            result.Append(HttpUtility.HtmlEncode(code[pos..]));
        return result.ToString();
    }
}

using DevPulse.Infrastructure.YouTube;
using FluentAssertions;

namespace DevPulse.Tests.YouTube;

public class SyntaxHighlighterTests
{
    [Fact]
    public void Highlights_keyword_in_red()
    {
        var result = SyntaxHighlighter.Highlight("let x = 1");
        result.Should().Contain("#ff7b72").And.Contain("let");
    }

    [Fact]
    public void Highlights_string_literal_in_blue()
    {
        var result = SyntaxHighlighter.Highlight("""var s = "hello";""");
        result.Should().Contain("#a5d6ff").And.Contain("hello");
    }

    [Fact]
    public void Highlights_line_comment_in_gray()
    {
        var result = SyntaxHighlighter.Highlight("// a comment");
        result.Should().Contain("#8b949e").And.Contain("a comment");
    }

    [Fact]
    public void Highlights_pascal_case_type_in_orange()
    {
        var result = SyntaxHighlighter.Highlight("Maybe<int>");
        result.Should().Contain("#ffa657").And.Contain("Maybe");
    }

    [Fact]
    public void Highlights_number_in_blue()
    {
        var result = SyntaxHighlighter.Highlight("let n = 42");
        result.Should().Contain("#79c0ff").And.Contain("42");
    }

    [Fact]
    public void Preserves_unhighlighted_text()
    {
        var result = SyntaxHighlighter.Highlight("x + y");
        result.Should().Contain("x").And.Contain("y").And.Contain("+");
    }

    [Fact]
    public void Html_encodes_plain_text()
    {
        var result = SyntaxHighlighter.Highlight("x > 0");
        result.Should().Contain("&gt;").And.NotContain("> 0");
    }

    [Fact]
    public void Does_not_highlight_url_scheme_as_comment()
    {
        var result = SyntaxHighlighter.Highlight("""var url = "https://example.com";""");
        result.Should().NotContainAny("<span style=\"color:#8b949e\">https");
    }

    [Fact]
    public void Returns_empty_for_empty_input()
    {
        SyntaxHighlighter.Highlight("").Should().BeEmpty();
    }
}

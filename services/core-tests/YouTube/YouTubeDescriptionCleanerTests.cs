using DevPulse.Infrastructure.YouTube;
using FluentAssertions;

namespace DevPulse.Tests.YouTube;

public class YouTubeDescriptionCleanerTests
{
    [Fact]
    public void Appends_Shorts_hashtag()
    {
        YouTubeDescriptionCleaner.Clean("hello").Should().EndWith("\n\n#Shorts");
    }

    [Fact]
    public void Strips_urls()
    {
        var result = YouTubeDescriptionCleaner.Clean("Read more at https://example.com today");
        result.Should().NotContain("https://");
        result.Should().Contain("Read more at");
    }

    [Fact]
    public void Strips_angle_brackets()
    {
        var result = YouTubeDescriptionCleaner.Clean("Option<T> or Result<T, E>");
        result.Should().NotContain("<").And.NotContain(">");
        result.Should().Contain("Option").And.Contain("Result");
    }

    [Fact]
    public void Strips_non_ascii()
    {
        var result = YouTubeDescriptionCleaner.Clean("Smart “curly” quotes");
        result.Should().NotContain("“").And.NotContain("”");
    }

    [Fact]
    public void Truncates_at_4500_characters()
    {
        var long_text = new string('a', 5000);
        var result = YouTubeDescriptionCleaner.Clean(long_text);
        result.Should().HaveLength(4500 + "\n\n#Shorts".Length);
    }

    [Fact]
    public void Preserves_newlines()
    {
        var result = YouTubeDescriptionCleaner.Clean("line one\nline two");
        result.Should().Contain("line one\nline two");
    }
}

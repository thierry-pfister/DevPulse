using DevPulse.Infrastructure.Publishers;
using FluentAssertions;

namespace DevPulse.Tests.Publishers;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("The Maybe Monad",              "the-maybe-monad")]
    [InlineData("Docker Layer Caching",         "docker-layer-caching")]
    [InlineData("JWT Tokens Demystified",       "jwt-tokens-demystified")]
    [InlineData("  Leading and Trailing  ",     "leading-and-trailing")]
    [InlineData("Special! @#$ Characters",      "special-characters")]
    [InlineData("Multiple   Spaces   Between",  "multiple-spaces-between")]
    [InlineData("Numbers 123 and Text",         "numbers-123-and-text")]
    [InlineData("C# vs F#",                     "c-vs-f")]
    [InlineData("UPPERCASE TITLE",              "uppercase-title")]
    public void From_produces_correct_slug(string input, string expected)
    {
        SlugGenerator.From(input).Should().Be(expected);
    }
}

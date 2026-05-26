using DevPulse.Infrastructure.Config;
using FluentAssertions;

namespace DevPulse.Tests.Config;

public class TopicConfigLoaderTests : IDisposable
{
    private readonly string _path = Path.GetTempFileName();

    public void Dispose() => File.Delete(_path);

    [Fact]
    public void Load_returns_all_non_skipped_and_skipped_entries()
    {
        File.WriteAllText(_path, """
            topics:
              - concept: "The Maybe Monad"
                tag: functional
                language: fsharp
                runnable: true
                priority: 1
                skip: false
              - concept: "Docker Layer Caching"
                tag: devops
                runnable: false
                priority: 2
                skip: true
            """);

        var entries = new TopicConfigLoader(_path).Load();

        entries.Should().HaveCount(2);
    }

    [Fact]
    public void Load_maps_fields_correctly()
    {
        File.WriteAllText(_path, """
            topics:
              - concept: "JWT Tokens Demystified"
                tag: security
                language: csharp
                runnable: false
                foreshadow_next: "RBAC"
                priority: 3
                skip: false
            """);

        var entry = new TopicConfigLoader(_path).Load().Single();

        entry.Concept.Should().Be("JWT Tokens Demystified");
        entry.Tag.Should().Be("security");
        entry.Language.Should().Be("csharp");
        entry.Runnable.Should().BeFalse();
        entry.ForeshadowNext.Should().Be("RBAC");
        entry.Priority.Should().Be(3);
        entry.Skip.Should().BeFalse();
    }

    [Fact]
    public void Load_handles_missing_optional_fields()
    {
        File.WriteAllText(_path, """
            topics:
              - concept: "Docker Basics"
                tag: devops
                runnable: false
                priority: 1
                skip: false
            """);

        var entry = new TopicConfigLoader(_path).Load().Single();

        entry.Language.Should().BeNull();
        entry.ForeshadowNext.Should().BeNull();
    }
}

using DevPulse.Domain.Episodes;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace DevPulse.Tests.Fixtures;

internal static class EpisodeFixtures
{
    internal static EpisodeOutput Simple(string title = "The Maybe Monad") =>
        new(
            article: new ArticleContent(
                title:            title,
                subtitle:         "You already use this — you just didn't have a name for it",
                realWorldAnchor:  "user?.email is a manual Maybe monad",
                body:             "# The Maybe Monad\n\nSome markdown content.",
                runnableSnippet:  FSharpOption<string>.None,
                imagePrompt:      "abstract monad illustration",
                foreshadow:       "Tomorrow: Result<T> — when None isn't enough.",
                tags:             ListModule.OfSeq(new[] { "functional", "fsharp" })
            ),
            reddit:  FSharpOption<RedditContent>.None,
            youTube: FSharpOption<YouTubeContent>.None
        );
}

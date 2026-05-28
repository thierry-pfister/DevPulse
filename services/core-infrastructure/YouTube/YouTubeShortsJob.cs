using DevPulse.Application.Episodes;
using DevPulse.Domain.Episodes;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class YouTubeShortsJob(
    IEpisodeRepository     repo,
    ITtsService            tts,
    ICodeScreenshotService screenshot,
    IVideoAssemblyService  video,
    IYouTubeUploadService  youTube,
    IR2VideoStorage        r2,
    ILogger<YouTubeShortsJob> logger)
{
    public async Task Execute(Guid episodeId)
    {
        var maybeEpisode = await repo.FindById(EpisodeId.NewEpisodeId(episodeId));
        if (maybeEpisode is null) return;

        var episode = maybeEpisode.Value;
        if (episode.Content is null || episode.Content.Value.YouTube is null) return;

        var yt      = episode.Content.Value.YouTube.Value;
        var article = episode.Content.Value.Article;

        var audioBytes = await tts.SynthesizeAsync(yt.Script);
        if (audioBytes is null)
        {
            logger.LogWarning("TTS failed for episode {Id}", episodeId);
            return;
        }

        var slides        = BuildSlides(article, yt);
        var images        = await screenshot.CaptureMultipleAsync(slides);
        var slidesWithDur = PairWithDurations(images, slides);

        if (slidesWithDur.Count == 0)
        {
            logger.LogWarning("All screenshots failed for episode {Id}", episodeId);
            return;
        }

        var videoBytes = await video.AssembleFromSlidesAsync(slidesWithDur, audioBytes);
        if (videoBytes is null)
        {
            logger.LogWarning("Video assembly failed for episode {Id}", episodeId);
            return;
        }

        var r2Url   = await r2.UploadVideoAsync(videoBytes, episodeId);
        var videoId = await youTube.UploadAsync(videoBytes, yt.Title, yt.Description, article.Tags);

        var storeUrl = videoId is not null ? $"https://youtube.com/shorts/{videoId}" : r2Url;
        if (storeUrl is null) return;

        var updated = EpisodeModule.setVideoUrl(storeUrl, episode);
        if (videoId is not null)
            updated = EpisodeModule.recordPlatformId("youtube", videoId, updated);

        await repo.Save(updated);
        logger.LogInformation("YouTube Short published for episode {Id}: {Url}", episodeId, storeUrl);
    }

    private static IReadOnlyList<SlideContent> BuildSlides(ArticleContent article, YouTubeContent yt)
    {
        var slides = new List<SlideContent>
        {
            new(SlideKind.Title, yt.Title, article.Subtitle),
        };

        if (!string.IsNullOrWhiteSpace(article.RealWorldAnchor))
            slides.Add(new SlideContent(SlideKind.Hook, "You already know this", article.RealWorldAnchor));

        var code = article.RunnableSnippet?.Value;
        if (!string.IsNullOrWhiteSpace(code))
        {
            foreach (var chunk in SplitCode(code, linesPerChunk: 8))
                slides.Add(new SlideContent(SlideKind.Code, article.Title, Code: chunk));
        }

        slides.Add(new SlideContent(SlideKind.Takeaway, "Tomorrow", article.Foreshadow));

        return slides
            .Select((s, i) => s with { SlideIndex = i, TotalSlides = slides.Count })
            .ToList();
    }

    private static IEnumerable<string> SplitCode(string code, int linesPerChunk)
    {
        var lines = code.Split('\n');
        for (var i = 0; i < lines.Length; i += linesPerChunk)
            yield return string.Join('\n', lines.Skip(i).Take(linesPerChunk));
    }

    private static IReadOnlyList<(byte[] Image, double DurationSeconds)> PairWithDurations(
        byte[]?[] images, IReadOnlyList<SlideContent> slides) =>
        images
            .Zip(slides)
            .Where(p => p.First is not null)
            .Select(p => (p.First!, DurationFor(p.Second)))
            .ToList();

    private static double DurationFor(SlideContent slide) => slide.Kind switch
    {
        SlideKind.Title    => 4.0,
        SlideKind.Hook     => 5.0,
        SlideKind.Takeaway => 60.0,
        _                  => 6.0,
    };
}

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DevPulse.Application.Episodes;
using DevPulse.Domain.Episodes;
using DevPulse.Infrastructure.ImageGeneration;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.YouTube;

public class YouTubeShortsJob(
    IEpisodeRepository      repo,
    ITtsService             tts,
    ICodeScreenshotService  screenshot,
    IVideoAssemblyService   video,
    R2Config                r2Config,
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

        byte[]? imageBytes = null;
        var snippet = article.RunnableSnippet != null
            ? article.RunnableSnippet.Value
            : string.Empty;

        imageBytes = await screenshot.CaptureAsync(snippet, article.Title);

        var videoBytes = await video.AssembleAsync(imageBytes!, audioBytes);
        if (videoBytes is null)
        {
            logger.LogWarning("Video assembly failed for episode {Id}", episodeId);
            return;
        }

        var url = await UploadToR2(videoBytes, episodeId);
        if (url is null) return;

        var updated = EpisodeModule.setVideoUrl(url, episode);
        await repo.Save(updated);

        logger.LogInformation("YouTube Short assembled for episode {Id}: {Url}", episodeId, url);
    }

    private async Task<string?> UploadToR2(byte[] videoBytes, Guid episodeId)
    {
        try
        {
            var s3Config = new AmazonS3Config { ServiceURL = r2Config.Endpoint, ForcePathStyle = true };
            var creds    = new BasicAWSCredentials(r2Config.AccessKeyId, r2Config.SecretAccessKey);
            using var s3 = new AmazonS3Client(creds, s3Config);

            var key = $"shorts/{episodeId}.mp4";
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName            = r2Config.BucketName,
                Key                   = key,
                InputStream           = new MemoryStream(videoBytes),
                ContentType           = "video/mp4",
                DisablePayloadSigning = true,
            });

            return $"{r2Config.PublicUrl.TrimEnd('/')}/{key}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "R2 upload failed for short {Id}", episodeId);
            return null;
        }
    }
}

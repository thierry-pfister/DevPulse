using DevPulse.Application.Episodes;
using DevPulse.Application.Generation;
using DevPulse.Domain.Episodes;
using DevPulse.Infrastructure.ImageGeneration;
using DevPulse.Infrastructure.Notifications;
using Hangfire;

using static DevPulse.Application.Generation.GenerateEpisodeCommands;

namespace DevPulse.Infrastructure.Scheduling;

public class GenerateEpisodeJob(
    IEpisodeRepository      repo,
    IClaudeClient           claude,
    IEmailNotifier          notifier,
    IImageGenerationService imageService,
    SchedulingConfig        config)
{
    public async Task Execute(Guid episodeId)
    {
        var cmd = new GenerateEpisodeCommand(EpisodeId.NewEpisodeId(episodeId), DateTimeOffset.UtcNow);

        var result = await generateEpisode(repo, claude, cmd);

        if (!result.IsOk) return;

        var episode = await AttachCoverImage(result.ResultValue);

        await notifier.SendDraftReadyAsync(episode);
        var delay = TimeSpan.FromMinutes(config.InterventionWindowMinutes);
        BackgroundJob.Schedule<AutoPublishJob>(j => j.Execute(episodeId), delay);
    }

    private async Task<Episode> AttachCoverImage(Episode episode)
    {
        if (episode.Content is null) return episode;

        var prompt = episode.Content.Value.Article.ImagePrompt;
        if (string.IsNullOrWhiteSpace(prompt)) return episode;

        var url = await imageService.GenerateAndStoreAsync(prompt, episode.Id.Item.ToString());
        if (url is null) return episode;

        var withImage = EpisodeModule.setCoverImage(url, episode);
        await repo.Save(withImage);
        return withImage;
    }
}

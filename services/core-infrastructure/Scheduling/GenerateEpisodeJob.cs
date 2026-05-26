using DevPulse.Application.Episodes;
using DevPulse.Application.Generation;
using DevPulse.Domain.Episodes;
using DevPulse.Infrastructure.Notifications;
using Hangfire;

using static DevPulse.Application.Generation.GenerateEpisodeCommands;

namespace DevPulse.Infrastructure.Scheduling;

public class GenerateEpisodeJob(
    IEpisodeRepository  repo,
    IClaudeClient       claude,
    IEmailNotifier      notifier,
    SchedulingConfig    config)
{
    public async Task Execute(Guid episodeId)
    {
        var cmd = new GenerateEpisodeCommand(EpisodeId.NewEpisodeId(episodeId), DateTimeOffset.UtcNow);

        var result = await generateEpisode(repo, claude, cmd);

        if (result.IsOk)
        {
            await notifier.SendDraftReadyAsync(result.ResultValue);
            var delay = TimeSpan.FromMinutes(config.InterventionWindowMinutes);
            BackgroundJob.Schedule<AutoPublishJob>(j => j.Execute(episodeId), delay);
        }
    }
}

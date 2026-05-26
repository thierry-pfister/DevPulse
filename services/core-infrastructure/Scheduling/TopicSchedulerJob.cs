using DevPulse.Application.Episodes;
using DevPulse.Infrastructure.Data;
using DevPulse.Infrastructure.TopicQueue;
using Hangfire;
using Microsoft.EntityFrameworkCore;

using static DevPulse.Application.Episodes.EpisodeCommands;

namespace DevPulse.Infrastructure.Scheduling;

public class TopicSchedulerJob(
    IEpisodeRepository   episodeRepo,
    ITopicQueueRepository topicQueue,
    AppDbContext          db,
    SchedulingConfig      config)
{
    public async Task Execute()
    {
        var tag = await PickMostOverdueTag();
        if (tag is null) return;

        var topic = await topicQueue.PickNextForTag(tag);
        if (topic is null) return;

        var publishAt = DateTimeOffset.UtcNow.AddMinutes(config.InterventionWindowMinutes);
        var language  = topic.Language is null
            ? Microsoft.FSharp.Core.FSharpOption<string>.None
            : Microsoft.FSharp.Core.FSharpOption<string>.Some(topic.Language);
        var cmd = new CreateEpisodeCommand(topic.Concept, topic.Tag, language, publishAt);

        var result = await createEpisode(episodeRepo, DateTimeOffset.UtcNow, cmd);
        if (!result.IsOk) return;

        await topicQueue.MarkSelectedAsync(topic.Id);
        BackgroundJob.Enqueue<GenerateEpisodeJob>(j => j.Execute(result.ResultValue.Id.Item));
    }

    private async Task<string?> PickMostOverdueTag()
    {
        var now            = DateTimeOffset.UtcNow;
        string? best       = null;
        double  maxOverdue = -1;

        foreach (var (tag, cadenceDays) in config.TagCadence)
        {
            var lastPublished = await db.Episodes
                .Where(e => e.Tag == tag && e.Status == "Published" && e.PublishedAt != null)
                .MaxAsync(e => (DateTimeOffset?)e.PublishedAt);

            var daysSince   = lastPublished.HasValue ? (now - lastPublished.Value).TotalDays : double.MaxValue;
            var overdueRatio = daysSince / cadenceDays;

            if (overdueRatio > maxOverdue)
            {
                maxOverdue = overdueRatio;
                best       = tag;
            }
        }
        return best;
    }
}

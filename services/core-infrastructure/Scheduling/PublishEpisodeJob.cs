using DevPulse.Application.Episodes;
using DevPulse.Application.Publishing;
using DevPulse.Domain.Episodes;
using DevPulse.Infrastructure.Notifications;

namespace DevPulse.Infrastructure.Scheduling;

public class PublishEpisodeJob(
    IEpisodeRepository      repo,
    IEnumerable<IPublisher> publishers,
    IEmailNotifier          notifier)
{
    public async Task Execute(Guid episodeId)
    {
        var maybeEpisode = await repo.FindById(EpisodeId.NewEpisodeId(episodeId));
        if (maybeEpisode is null) return;

        var episode = maybeEpisode.Value;
        if (!episode.Status.IsDraft || episode.Content is null) return;

        // Publish to all platforms and accumulate IDs
        foreach (var publisher in publishers)
        {
            var result = await publisher.PublishAsync(episode.Content.Value);
            if (result is PublishResult.Published pub)
                episode = EpisodeModule.recordPlatformId(publisher.Name, pub.platformId, episode);
        }

        // Approve (sets Published status + PublishedAt)
        var approveResult = EpisodeModule.approve(DateTimeOffset.UtcNow, episode);
        if (!approveResult.IsOk) return;

        await repo.Save(approveResult.ResultValue);
        await notifier.SendPublishedAsync(approveResult.ResultValue);
    }
}

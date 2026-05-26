using DevPulse.Application.Episodes;
using DevPulse.Domain.Episodes;
using Hangfire;

namespace DevPulse.Infrastructure.Scheduling;

public class AutoPublishJob(IEpisodeRepository repo)
{
    public async Task Execute(Guid episodeId)
    {
        var maybeEpisode = await repo.FindById(EpisodeId.NewEpisodeId(episodeId));
        if (maybeEpisode is null) return;

        if (!maybeEpisode.Value.Status.IsDraft) return;

        BackgroundJob.Enqueue<PublishEpisodeJob>(j => j.Execute(episodeId));
    }
}

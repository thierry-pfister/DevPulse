using DevPulse.Application.Generation;
using DevPulse.Infrastructure.TopicQueue;
using Microsoft.FSharp.Collections;
using Microsoft.Extensions.Logging;

namespace DevPulse.Infrastructure.Scheduling;

public class BacklogTopperJob(
    ITopicQueueRepository topicQueue,
    IClaudeClient         claude,
    SchedulingConfig      config,
    ILogger<BacklogTopperJob> logger)
{
    public async Task Execute()
    {
        foreach (var tag in config.TagCadence.Keys)
        {
            var pending = await topicQueue.PendingCountForTag(tag);
            if (pending >= config.BacklogMinPerTag) continue;

            var needed      = config.BacklogMinPerTag - pending;
            var existing    = await topicQueue.GetConceptsForTag(tag);
            var existingFs  = ListModule.OfSeq(existing);

            logger.LogInformation("Topping up tag '{Tag}': need {Needed} more topics", tag, needed);

            var suggestions = await claude.SuggestTopicsAsync(tag, needed, existingFs);
            var asList      = suggestions.ToList();
            if (asList.Count == 0) continue;

            var entities = asList.Select(concept => new TopicQueueEntity
            {
                Id        = Guid.NewGuid(),
                Concept   = concept,
                Tag       = tag,
                Runnable  = true,
                Priority  = 100,
                Status    = "pending",
                Source    = "generated",
                CreatedAt = DateTimeOffset.UtcNow
            });

            await topicQueue.AddTopicsAsync(entities);
            logger.LogInformation("Added {Count} topics for tag '{Tag}'", asList.Count, tag);
        }
    }
}

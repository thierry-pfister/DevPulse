using DevPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.TopicQueue;

public class TopicQueueRepository(AppDbContext db) : ITopicQueueRepository
{
    public Task<TopicQueueEntity?> PickNextForTag(string tag) =>
        db.TopicQueue
            .Where(t => t.Tag == tag && t.Status == "pending")
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

    public Task<int> PendingCountForTag(string tag) =>
        db.TopicQueue.CountAsync(t => t.Tag == tag && t.Status == "pending");

    public Task<List<string>> GetConceptsForTag(string tag) =>
        db.TopicQueue
            .Where(t => t.Tag == tag)
            .Select(t => t.Concept)
            .ToListAsync();

    public async Task AddTopicsAsync(IEnumerable<TopicQueueEntity> topics)
    {
        db.TopicQueue.AddRange(topics);
        await db.SaveChangesAsync();
    }

    public async Task MarkSelectedAsync(Guid id)
    {
        var topic = await db.TopicQueue.FindAsync(id);
        if (topic is null) return;
        topic.Status = "selected";
        await db.SaveChangesAsync();
    }
}

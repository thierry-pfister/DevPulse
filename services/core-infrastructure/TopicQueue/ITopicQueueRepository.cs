namespace DevPulse.Infrastructure.TopicQueue;

public interface ITopicQueueRepository
{
    Task<TopicQueueEntity?>   PickNextForTag(string tag);
    Task<int>                 PendingCountForTag(string tag);
    Task<List<string>>        GetConceptsForTag(string tag);
    Task                      AddTopicsAsync(IEnumerable<TopicQueueEntity> topics);
    Task                      MarkSelectedAsync(Guid id);
}

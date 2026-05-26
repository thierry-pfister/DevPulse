namespace DevPulse.Infrastructure.TopicQueue;

public class TopicQueueEntity
{
    public Guid    Id            { get; set; }
    public string  Concept       { get; set; } = "";
    public string  Tag           { get; set; } = "";
    public string? Language      { get; set; }
    public bool    Runnable      { get; set; } = true;
    public string? ForeshadowNext { get; set; }
    public int     Priority      { get; set; }
    public string  Status        { get; set; } = "pending";  // pending | selected | skipped
    public string  Source        { get; set; } = "manual";   // manual | generated
    public DateTimeOffset CreatedAt { get; set; }
}

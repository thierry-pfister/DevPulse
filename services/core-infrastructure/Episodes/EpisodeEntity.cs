namespace DevPulse.Infrastructure.Episodes;

public class EpisodeEntity
{
    public Guid Id { get; set; }
    public string Concept { get; set; } = "";
    public string Tag { get; set; } = "";
    public string? Language { get; set; }
    public int EpisodeNumber { get; set; }
    public string Status { get; set; } = "queued";
    public string? ContentJson { get; set; }
    public bool WasEdited { get; set; }
    public DateTimeOffset? PublishAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? PlatformIdsJson { get; set; }
    public Guid? PreviousEpisodeId { get; set; }
    public DateTimeOffset? GeneratedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

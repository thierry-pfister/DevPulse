namespace DevPulse.Infrastructure.Config;

public record TopicEntry
{
    public string Concept     { get; init; } = "";
    public string Tag         { get; init; } = "";
    public string? Language   { get; init; }
    public bool Runnable      { get; init; }
    public string? ForeshadowNext { get; init; }
    public int Priority       { get; init; }
    public bool Skip          { get; init; }
}

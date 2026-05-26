namespace DevPulse.Api.Models;

public record ArticleContentResponse(
    string Title,
    string Subtitle,
    string RealWorldAnchor,
    string Body,
    string? RunnableSnippet,
    string ImagePrompt,
    string? CoverImageUrl,
    string Foreshadow,
    List<string> Tags);

public record EpisodeResponse(
    Guid Id,
    string Concept,
    string Tag,
    string? Language,
    int EpisodeNumber,
    string Status,
    bool WasEdited,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset CreatedAt,
    Dictionary<string, string> PlatformIds,
    ArticleContentResponse? Content);

public record DelayRequest(DateTimeOffset NewPublishAt);

public record CreateEpisodeRequest(
    string Concept,
    string Tag,
    string? Language,
    DateTimeOffset? PublishAt);

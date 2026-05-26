namespace DevPulse.Infrastructure.Episodes;

record ArticleContentDto(
    string Title,
    string Subtitle,
    string RealWorldAnchor,
    string Body,
    string? RunnableSnippet,
    string ImagePrompt,
    string Foreshadow,
    List<string> Tags);

record RedditContentDto(string Title, string Body);

record YouTubeContentDto(string Title, string Description, string Script);

record EpisodeContentDto(
    ArticleContentDto Article,
    RedditContentDto? Reddit,
    YouTubeContentDto? YouTube);

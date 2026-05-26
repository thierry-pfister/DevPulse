using System.Text.Json;
using DevPulse.Domain.Episodes;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace DevPulse.Infrastructure.Episodes;

internal static class EpisodeMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly FSharpMap<string, string> EmptyMap =
        new(Array.Empty<Tuple<string, string>>());

    // ── Entity → Domain ───────────────────────────────────────────────────────

    internal static Episode ToDomain(EpisodeEntity e) => new(
        id:               EpisodeId.NewEpisodeId(e.Id),
        concept:          e.Concept,
        tag:              e.Tag,
        language:         ToOpt(e.Language),
        episodeNumber:    e.EpisodeNumber,
        status:           ParseStatus(e.Status),
        content:          e.ContentJson is null
            ? FSharpOption<EpisodeOutput>.None
            : FSharpOption<EpisodeOutput>.Some(DeserializeContent(e.ContentJson)),
        wasEdited:        e.WasEdited,
        publishAt:        ToOpt(e.PublishAt),
        publishedAt:      ToOpt(e.PublishedAt),
        platformIds:      ParsePlatformIds(e.PlatformIdsJson),
        previousEpisodeId: e.PreviousEpisodeId.HasValue
            ? FSharpOption<EpisodeId>.Some(EpisodeId.NewEpisodeId(e.PreviousEpisodeId.Value))
            : FSharpOption<EpisodeId>.None,
        generatedAt:      ToOpt(e.GeneratedAt),
        createdAt:        e.CreatedAt
    );

    // ── Domain → Entity ───────────────────────────────────────────────────────

    internal static EpisodeEntity ToEntity(Episode ep) => new()
    {
        Id               = ep.Id.Item,
        Concept          = ep.Concept,
        Tag              = ep.Tag,
        Language         = ep.Language?.Value,
        EpisodeNumber    = ep.EpisodeNumber,
        Status           = StatusToString(ep.Status),
        ContentJson      = ep.Content is null ? null
            : JsonSerializer.Serialize(SerializeContent(ep.Content.Value), JsonOptions),
        WasEdited        = ep.WasEdited,
        PublishAt        = ep.PublishAt?.Value,
        PublishedAt      = ep.PublishedAt?.Value,
        PlatformIdsJson  = SerializePlatformIds(ep.PlatformIds),
        PreviousEpisodeId = ep.PreviousEpisodeId?.Value.Item,
        GeneratedAt      = ep.GeneratedAt?.Value,
        CreatedAt        = ep.CreatedAt
    };

    // ── Status ────────────────────────────────────────────────────────────────

    internal static string StatusToString(EpisodeStatus s) =>
        s.IsQueued     ? "Queued"     :
        s.IsGenerating ? "Generating" :
        s.IsDraft      ? "Draft"      :
        s.IsPublished  ? "Published"  :
        s.IsSkipped    ? "Skipped"    :
        s.IsFailed     ? "Failed"     :
        throw new ArgumentException($"Unknown status: {s}");

    private static EpisodeStatus ParseStatus(string s) => s switch
    {
        "Queued"     => EpisodeStatus.Queued,
        "Generating" => EpisodeStatus.Generating,
        "Draft"      => EpisodeStatus.Draft,
        "Published"  => EpisodeStatus.Published,
        "Skipped"    => EpisodeStatus.Skipped,
        "Failed"     => EpisodeStatus.Failed,
        _ => throw new ArgumentException($"Unknown status: {s}")
    };

    // ── PlatformIds ───────────────────────────────────────────────────────────

    private static FSharpMap<string, string> ParsePlatformIds(string? json)
    {
        if (json is null) return EmptyMap;
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? [];
        return new FSharpMap<string, string>(dict.Select(kv => Tuple.Create(kv.Key, kv.Value)));
    }

    private static string? SerializePlatformIds(FSharpMap<string, string> map) =>
        map.IsEmpty ? null : JsonSerializer.Serialize(
            map.ToDictionary(kv => kv.Key, kv => kv.Value), JsonOptions);

    // ── Content ───────────────────────────────────────────────────────────────

    private static EpisodeContentDto SerializeContent(EpisodeOutput o) => new(
        Article: new ArticleContentDto(
            Title:           o.Article.Title,
            Subtitle:        o.Article.Subtitle,
            RealWorldAnchor: o.Article.RealWorldAnchor,
            Body:            o.Article.Body,
            RunnableSnippet: o.Article.RunnableSnippet?.Value,
            ImagePrompt:     o.Article.ImagePrompt,
            Foreshadow:      o.Article.Foreshadow,
            Tags:            o.Article.Tags.ToList()),
        Reddit:  o.Reddit  is null ? null : new RedditContentDto(o.Reddit.Value.Title, o.Reddit.Value.Body),
        YouTube: o.YouTube is null ? null : new YouTubeContentDto(o.YouTube.Value.Title, o.YouTube.Value.Description, o.YouTube.Value.Script)
    );

    private static EpisodeOutput DeserializeContent(string json)
    {
        var dto = JsonSerializer.Deserialize<EpisodeContentDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize episode content");

        var article = new ArticleContent(
            title:           dto.Article.Title,
            subtitle:        dto.Article.Subtitle,
            realWorldAnchor: dto.Article.RealWorldAnchor,
            body:            dto.Article.Body,
            runnableSnippet: dto.Article.RunnableSnippet is null
                ? FSharpOption<string>.None
                : FSharpOption<string>.Some(dto.Article.RunnableSnippet),
            imagePrompt:     dto.Article.ImagePrompt,
            foreshadow:      dto.Article.Foreshadow,
            tags:            ListModule.OfSeq(dto.Article.Tags));

        return new EpisodeOutput(
            article: article,
            reddit:  dto.Reddit is null ? FSharpOption<RedditContent>.None
                : FSharpOption<RedditContent>.Some(new RedditContent(dto.Reddit.Title, dto.Reddit.Body)),
            youTube: dto.YouTube is null ? FSharpOption<YouTubeContent>.None
                : FSharpOption<YouTubeContent>.Some(new YouTubeContent(dto.YouTube.Title, dto.YouTube.Description, dto.YouTube.Script)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FSharpOption<T> ToOpt<T>(T? value) where T : class =>
        value is null ? FSharpOption<T>.None : FSharpOption<T>.Some(value);

    private static FSharpOption<T> ToOpt<T>(T? value) where T : struct =>
        value.HasValue ? FSharpOption<T>.Some(value.Value) : FSharpOption<T>.None;
}

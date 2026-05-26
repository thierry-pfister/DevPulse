using DevPulse.Api.Models;
using DevPulse.Application.Episodes;
using DevPulse.Infrastructure.Scheduling;
using static DevPulse.Application.Episodes.EpisodeCommands;
using static DevPulse.Application.Episodes.EpisodeQueries;
using DevPulse.Domain.Episodes;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;

namespace DevPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EpisodesController(IEpisodeRepository repo) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<EpisodeResponse>> Create([FromBody] CreateEpisodeRequest request)
    {
        var language  = request.Language is null
            ? FSharpOption<string>.None
            : FSharpOption<string>.Some(request.Language);
        var publishAt = request.PublishAt ?? DateTimeOffset.UtcNow.AddMinutes(30);
        var cmd       = new CreateEpisodeCommand(request.Concept, request.Tag, language, publishAt);
        var result    = await createEpisode(repo, DateTimeOffset.UtcNow, cmd);
        return result.IsOk
            ? CreatedAtAction(nameof(Get), new { id = result.ResultValue.Id.Item }, ToResponse(result.ResultValue))
            : MapError(result.ErrorValue);
    }

    [HttpPost("{id:guid}/generate")]
    [Authorize]
    public async Task<ActionResult> Generate(Guid id)
    {
        var ep = await repo.FindById(EpisodeId.NewEpisodeId(id));
        if (ep is null) return NotFound();
        BackgroundJob.Enqueue<GenerateEpisodeJob>(j => j.Execute(id));
        return Accepted();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EpisodeResponse>>> List([FromQuery] string? status = null)
    {
        var episodes = await listEpisodes(repo, ParseStatusFilter(status));
        return Ok(episodes.Select(ToResponse));
    }

    [HttpGet("draft")]
    public async Task<ActionResult<EpisodeResponse>> GetDraft()
    {
        var ep = await getDraft(repo);
        return ep is null ? NotFound() : Ok(ToResponse(ep.Value));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EpisodeResponse>> Get(Guid id)
    {
        var result = await getEpisode(repo, EpisodeId.NewEpisodeId(id));
        return result.IsOk ? Ok(ToResponse(result.ResultValue)) : MapError(result.ErrorValue);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize]
    public async Task<ActionResult> Approve(Guid id)
    {
        var ep = await repo.FindById(EpisodeId.NewEpisodeId(id));
        if (ep is null) return NotFound();
        BackgroundJob.Enqueue<PublishEpisodeJob>(j => j.Execute(id));
        return Accepted();
    }

    [HttpPost("{id:guid}/skip")]
    [Authorize]
    public async Task<ActionResult> Skip(Guid id)
    {
        var cmd = new SkipEpisodeCommand(EpisodeId.NewEpisodeId(id));
        var result = await skipEpisode(repo, cmd);
        return result.IsOk ? NoContent() : MapError(result.ErrorValue);
    }

    [HttpPost("{id:guid}/delay")]
    [Authorize]
    public async Task<ActionResult> Delay(Guid id, [FromBody] DelayRequest request)
    {
        var cmd = new DelayEpisodeCommand(EpisodeId.NewEpisodeId(id), request.NewPublishAt);
        var result = await delayEpisode(repo, cmd);
        return result.IsOk ? NoContent() : MapError(result.ErrorValue);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static EpisodeResponse ToResponse(Episode ep) => new(
        Id:            ep.Id.Item,
        Concept:       ep.Concept,
        Tag:           ep.Tag,
        Language:      ep.Language?.Value,
        EpisodeNumber: ep.EpisodeNumber,
        Status:        StatusString(ep.Status),
        WasEdited:     ep.WasEdited,
        PublishAt:     ep.PublishAt?.Value,
        PublishedAt:   ep.PublishedAt?.Value,
        GeneratedAt:   ep.GeneratedAt?.Value,
        CreatedAt:     ep.CreatedAt,
        PlatformIds:   ep.PlatformIds.ToDictionary(kv => kv.Key, kv => kv.Value),
        Content:       ep.Content is null ? null : ToArticleContent(ep.Content.Value.Article)
    );

    private static ArticleContentResponse ToArticleContent(ArticleContent a) => new(
        Title:           a.Title,
        Subtitle:        a.Subtitle,
        RealWorldAnchor: a.RealWorldAnchor,
        Body:            a.Body,
        RunnableSnippet: a.RunnableSnippet?.Value,
        ImagePrompt:     a.ImagePrompt,
        CoverImageUrl:   a.CoverImageUrl?.Value,
        Foreshadow:      a.Foreshadow,
        Tags:            a.Tags.ToList()
    );

    private static string StatusString(EpisodeStatus s) =>
        s.IsQueued ? "Queued" :
        s.IsGenerating ? "Generating" :
        s.IsDraft ? "Draft" :
        s.IsPublished ? "Published" :
        s.IsSkipped ? "Skipped" :
        s.IsFailed ? "Failed" : "Unknown";

    private static FSharpOption<EpisodeStatus> ParseStatusFilter(string? s) => s switch
    {
        "Queued"     => FSharpOption<EpisodeStatus>.Some(EpisodeStatus.Queued),
        "Generating" => FSharpOption<EpisodeStatus>.Some(EpisodeStatus.Generating),
        "Draft"      => FSharpOption<EpisodeStatus>.Some(EpisodeStatus.Draft),
        "Published"  => FSharpOption<EpisodeStatus>.Some(EpisodeStatus.Published),
        "Skipped"    => FSharpOption<EpisodeStatus>.Some(EpisodeStatus.Skipped),
        "Failed"     => FSharpOption<EpisodeStatus>.Some(EpisodeStatus.Failed),
        _            => FSharpOption<EpisodeStatus>.None
    };

    private ActionResult MapError(EpisodeApplicationError error) => error switch
    {
        EpisodeApplicationError.ValidationError ve => BadRequest(new { error = ve.Item }),
        EpisodeApplicationError.NotFound nf        => NotFound(new { error = nf.Item }),
        EpisodeApplicationError.Conflict c         => Conflict(new { error = c.Item }),
        EpisodeApplicationError.DomainError de     => UnprocessableEntity(new { error = de.Item }),
        _                                          => StatusCode(500)
    };
}

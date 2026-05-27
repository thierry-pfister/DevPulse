namespace DevPulse.Domain.Episodes

open System

type EpisodeId = EpisodeId of Guid

type EpisodeStatus =
    | Queued
    | Generating
    | Draft
    | Published
    | Skipped
    | Failed

type Episode = {
    Id:               EpisodeId
    Concept:          string
    Tag:              string
    Language:         string option
    EpisodeNumber:    int
    Status:           EpisodeStatus
    Content:          EpisodeOutput option
    WasEdited:        bool
    PublishAt:        DateTimeOffset option
    PublishedAt:      DateTimeOffset option
    PlatformIds:      Map<string, string>
    PreviousEpisodeId: EpisodeId option
    GeneratedAt:      DateTimeOffset option
    CreatedAt:        DateTimeOffset
}

module Episode =

    let create (id: EpisodeId) (concept: string) (tag: string) (language: string option)
               (episodeNumber: int) (publishAt: DateTimeOffset) (now: DateTimeOffset) : Episode =
        { Id                = id
          Concept           = concept
          Tag               = tag
          Language          = language
          EpisodeNumber     = episodeNumber
          Status            = Queued
          Content           = None
          WasEdited         = false
          PublishAt         = Some publishAt
          PublishedAt       = None
          PlatformIds       = Map.empty
          PreviousEpisodeId = None
          GeneratedAt       = None
          CreatedAt         = now }

    let startGeneration (episode: Episode) : Result<Episode, string> =
        match episode.Status with
        | Queued -> Ok { episode with Status = Generating }
        | s      -> Error $"Cannot start generation from status {s}"

    let complete (content: EpisodeOutput) (now: DateTimeOffset) (episode: Episode) : Result<Episode, string> =
        match episode.Status with
        | Generating -> Ok { episode with Status = Draft; Content = Some content; GeneratedAt = Some now }
        | s          -> Error $"Cannot complete generation from status {s}"

    let fail (episode: Episode) : Result<Episode, string> =
        match episode.Status with
        | Generating -> Ok { episode with Status = Failed }
        | s          -> Error $"Cannot fail from status {s}"

    let approve (now: DateTimeOffset) (episode: Episode) : Result<Episode, string> =
        match episode.Status with
        | Draft -> Ok { episode with Status = Published; PublishedAt = Some now }
        | s     -> Error $"Cannot approve episode with status {s}"

    let skip (episode: Episode) : Result<Episode, string> =
        match episode.Status with
        | Draft -> Ok { episode with Status = Skipped }
        | s     -> Error $"Cannot skip episode with status {s}"

    let delay (newPublishAt: DateTimeOffset) (episode: Episode) : Result<Episode, string> =
        match episode.Status with
        | Draft -> Ok { episode with PublishAt = Some newPublishAt }
        | s     -> Error $"Cannot delay episode with status {s}"

    let setVideoUrl (url: string) (episode: Episode) : Episode =
        match episode.Content with
        | None -> episode
        | Some output ->
            match output.YouTube with
            | None -> episode
            | Some yt ->
                let updatedYt = { yt with VideoUrl = Some url }
                { episode with Content = Some { output with YouTube = Some updatedYt } }

    let setCoverImage (url: string) (episode: Episode) : Episode =
        match episode.Content with
        | None -> episode
        | Some output ->
            let updatedArticle = { output.Article with CoverImageUrl = Some url }
            { episode with Content = Some { output with Article = updatedArticle } }

    let recordPlatformId (platform: string) (platformId: string) (episode: Episode) : Episode =
        { episode with PlatformIds = Map.add platform platformId episode.PlatformIds }

    let markEdited (episode: Episode) : Episode =
        { episode with WasEdited = true }

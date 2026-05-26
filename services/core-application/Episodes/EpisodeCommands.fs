module DevPulse.Application.Episodes.EpisodeCommands

open System
open System.Threading.Tasks
open DevPulse.Domain.Episodes

type CreateEpisodeCommand = {
    Concept:       string
    Tag:           string
    Language:      string option
    PublishAt:     DateTimeOffset
}

type ApproveEpisodeCommand = { EpisodeId: EpisodeId; Now: DateTimeOffset }
type SkipEpisodeCommand    = { EpisodeId: EpisodeId }
type DelayEpisodeCommand   = { EpisodeId: EpisodeId; NewPublishAt: DateTimeOffset }

let createEpisode
        (repo: IEpisodeRepository)
        (now:  DateTimeOffset)
        (cmd:  CreateEpisodeCommand)
        : Task<Result<Episode, EpisodeApplicationError>> =
    task {
        let! n = repo.NextNumber()
        let ep = Episode.create (EpisodeId(Guid.NewGuid())) cmd.Concept cmd.Tag cmd.Language n cmd.PublishAt now
        do! repo.Save ep
        return Ok ep
    }

let approveEpisode
        (repo: IEpisodeRepository)
        (cmd:  ApproveEpisodeCommand)
        : Task<Result<Episode, EpisodeApplicationError>> =
    task {
        let! found = repo.FindById cmd.EpisodeId
        match found with
        | None -> return Error (NotFound $"Episode {cmd.EpisodeId} not found")
        | Some ep ->
            match Episode.approve cmd.Now ep with
            | Error msg -> return Error (DomainError msg)
            | Ok updated ->
                do! repo.Save updated
                return Ok updated
    }

let skipEpisode
        (repo: IEpisodeRepository)
        (cmd:  SkipEpisodeCommand)
        : Task<Result<Episode, EpisodeApplicationError>> =
    task {
        let! found = repo.FindById cmd.EpisodeId
        match found with
        | None -> return Error (NotFound $"Episode {cmd.EpisodeId} not found")
        | Some ep ->
            match Episode.skip ep with
            | Error msg -> return Error (DomainError msg)
            | Ok updated ->
                do! repo.Save updated
                return Ok updated
    }

let delayEpisode
        (repo: IEpisodeRepository)
        (cmd:  DelayEpisodeCommand)
        : Task<Result<Episode, EpisodeApplicationError>> =
    task {
        let! found = repo.FindById cmd.EpisodeId
        match found with
        | None -> return Error (NotFound $"Episode {cmd.EpisodeId} not found")
        | Some ep ->
            match Episode.delay cmd.NewPublishAt ep with
            | Error msg -> return Error (DomainError msg)
            | Ok updated ->
                do! repo.Save updated
                return Ok updated
    }

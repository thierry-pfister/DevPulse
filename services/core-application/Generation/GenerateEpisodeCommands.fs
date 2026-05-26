module DevPulse.Application.Generation.GenerateEpisodeCommands

open System
open System.Threading.Tasks
open DevPulse.Domain.Episodes
open DevPulse.Application.Episodes
open DevPulse.Application.Generation

type GenerateEpisodeCommand = {
    EpisodeId : EpisodeId
    Now       : DateTimeOffset
}

let generateEpisode
    (repo   : IEpisodeRepository)
    (claude : IClaudeClient)
    (cmd    : GenerateEpisodeCommand)
    : Task<Result<Episode, EpisodeApplicationError>> =
    task {
        match! repo.FindById cmd.EpisodeId with
        | None ->
            return Error (NotFound "Episode not found")
        | Some episode ->
            match Episode.startGeneration episode with
            | Error msg ->
                return Error (DomainError msg)
            | Ok generating ->
                do! repo.Save generating
                let! recent        = repo.FindRecentPublishedByTag episode.Tag 10
                let recentConcepts = recent |> List.map (fun e -> e.Concept)
                let input : GenerationInput = {
                    Concept             = episode.Concept
                    Tag                 = episode.Tag
                    Language            = episode.Language
                    EpisodeNumber       = episode.EpisodeNumber
                    RealWorldAnchorHint = None
                    Runnable            = true
                    ForeshadowTopic     = None
                    Tone                = "precise, practical, no fluff"
                    TargetAudience      = "intermediate developers"
                    RecentConcepts      = recentConcepts
                }
                let! claudeResult = claude.Generate input
                match claudeResult with
                | Error msg ->
                    match Episode.fail generating with
                    | Ok failed -> do! repo.Save failed
                    | Error _   -> ()
                    return Error (DomainError $"Generation failed: {msg}")
                | Ok output ->
                    match Episode.complete output cmd.Now generating with
                    | Error msg ->
                        match Episode.fail generating with
                        | Ok failed -> do! repo.Save failed
                        | Error _   -> ()
                        return Error (DomainError msg)
                    | Ok draft ->
                        do! repo.Save draft
                        return Ok draft
    }

module DevPulse.Application.Tests.Episodes.EpisodeCommandTests

open System
open Xunit
open FsUnit.Xunit
open DevPulse.Domain.Episodes
open DevPulse.Application.Episodes
open DevPulse.Application.Episodes.EpisodeCommands
open DevPulse.Application.Tests.Episodes.FakeEpisodeRepository

// ── Helpers ──────────────────────────────────────────────────────────────────

let private now = DateTimeOffset.UtcNow

let private makeCmd concept tag : CreateEpisodeCommand =
    { Concept   = concept
      Tag       = tag
      Language  = None
      PublishAt = now.AddHours(1) }

let private freshDraftEp () : Episode =
    { Id               = EpisodeId(Guid.NewGuid())
      Concept          = "Test"
      Tag              = "functional"
      Language         = None
      EpisodeNumber    = 1
      Status           = Draft
      Content          = None
      WasEdited        = false
      PublishAt        = Some (now.AddHours(1))
      PublishedAt      = None
      PlatformIds      = Map.empty
      PreviousEpisodeId = None
      GeneratedAt      = None
      CreatedAt        = now }

// ── createEpisode ─────────────────────────────────────────────────────────────

[<Fact>]
let ``createEpisode saves episode with Queued status`` () =
    task {
        let repo = FakeEpisodeRepository()
        let! result = createEpisode repo now (makeCmd "Maybe Monad" "functional")
        match result with
        | Ok ep ->
            ep.Status  |> should equal Queued
            ep.Concept |> should equal "Maybe Monad"
            ep.Tag     |> should equal "functional"
            repo.Store |> List.length |> should equal 1
        | Error e -> failwith $"Unexpected error: {e}"
    }

[<Fact>]
let ``createEpisode assigns sequential episode numbers`` () =
    task {
        let repo = FakeEpisodeRepository()
        let! r1 = createEpisode repo now (makeCmd "Concept A" "functional")
        let! r2 = createEpisode repo now (makeCmd "Concept B" "devops")
        match r1, r2 with
        | Ok ep1, Ok ep2 ->
            ep1.EpisodeNumber |> should equal 1
            ep2.EpisodeNumber |> should equal 2
        | _ -> failwith "Expected both Ok"
    }

[<Fact>]
let ``createEpisode stores language when provided`` () =
    task {
        let cmd : CreateEpisodeCommand =
            { Concept   = "Concept"
              Tag       = "functional"
              Language  = Some "fsharp"
              PublishAt = now.AddHours(1) }
        let repo = FakeEpisodeRepository()
        let! result = createEpisode repo now cmd
        match result with
        | Ok ep -> ep.Language |> should equal (Some "fsharp")
        | Error e -> failwith $"Unexpected error: {e}"
    }

// ── approveEpisode ────────────────────────────────────────────────────────────

[<Fact>]
let ``approveEpisode transitions Draft to Published`` () =
    task {
        let ep = freshDraftEp ()
        let repo = FakeEpisodeRepository(seed = [ep])
        let cmd : ApproveEpisodeCommand = { EpisodeId = ep.Id; Now = now }
        let! result = approveEpisode repo cmd
        match result with
        | Ok updated ->
            updated.Status      |> should equal Published
            updated.PublishedAt |> should not' (equal None)
        | Error e -> failwith $"Unexpected error: {e}"
    }

[<Fact>]
let ``approveEpisode returns NotFound for unknown id`` () =
    task {
        let repo = FakeEpisodeRepository()
        let cmd : ApproveEpisodeCommand = { EpisodeId = EpisodeId(Guid.NewGuid()); Now = now }
        let! result = approveEpisode repo cmd
        match result with
        | Error (NotFound _) -> ()
        | _ -> failwith "Expected NotFound"
    }

[<Fact>]
let ``approveEpisode returns DomainError for non-Draft episode`` () =
    task {
        let ep = { freshDraftEp () with Status = Published }
        let repo = FakeEpisodeRepository(seed = [ep])
        let cmd : ApproveEpisodeCommand = { EpisodeId = ep.Id; Now = now }
        let! result = approveEpisode repo cmd
        match result with
        | Error (DomainError _) -> ()
        | _ -> failwith "Expected DomainError"
    }

// ── skipEpisode ───────────────────────────────────────────────────────────────

[<Fact>]
let ``skipEpisode transitions Draft to Skipped`` () =
    task {
        let ep = freshDraftEp ()
        let repo = FakeEpisodeRepository(seed = [ep])
        let cmd : SkipEpisodeCommand = { EpisodeId = ep.Id }
        let! result = skipEpisode repo cmd
        match result with
        | Ok updated -> updated.Status |> should equal Skipped
        | Error e    -> failwith $"Unexpected error: {e}"
    }

[<Fact>]
let ``skipEpisode returns NotFound for unknown id`` () =
    task {
        let repo = FakeEpisodeRepository()
        let cmd : SkipEpisodeCommand = { EpisodeId = EpisodeId(Guid.NewGuid()) }
        let! result = skipEpisode repo cmd
        match result with
        | Error (NotFound _) -> ()
        | _ -> failwith "Expected NotFound"
    }

// ── delayEpisode ──────────────────────────────────────────────────────────────

[<Fact>]
let ``delayEpisode updates PublishAt on Draft episode`` () =
    task {
        let ep = freshDraftEp ()
        let newTime = now.AddHours(48)
        let repo = FakeEpisodeRepository(seed = [ep])
        let cmd : DelayEpisodeCommand = { EpisodeId = ep.Id; NewPublishAt = newTime }
        let! result = delayEpisode repo cmd
        match result with
        | Ok updated -> updated.PublishAt |> should equal (Some newTime)
        | Error e    -> failwith $"Unexpected error: {e}"
    }

[<Fact>]
let ``delayEpisode returns NotFound for unknown id`` () =
    task {
        let repo = FakeEpisodeRepository()
        let cmd : DelayEpisodeCommand = { EpisodeId = EpisodeId(Guid.NewGuid()); NewPublishAt = now }
        let! result = delayEpisode repo cmd
        match result with
        | Error (NotFound _) -> ()
        | _ -> failwith "Expected NotFound"
    }

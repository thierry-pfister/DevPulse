module DevPulse.Application.Tests.Episodes.EpisodeQueryTests

open System
open Xunit
open FsUnit.Xunit
open DevPulse.Domain.Episodes
open DevPulse.Application.Episodes
open DevPulse.Application.Episodes.EpisodeQueries
open DevPulse.Application.Tests.Episodes.FakeEpisodeRepository

// ── Helpers ──────────────────────────────────────────────────────────────────

let private now = DateTimeOffset.UtcNow

let private makeEp status =
    { Id               = EpisodeId(Guid.NewGuid())
      Concept          = "Test"
      Tag              = "functional"
      Language         = None
      EpisodeNumber    = 1
      Status           = status
      Content          = None
      WasEdited        = false
      PublishAt        = Some (now.AddHours(1))
      PublishedAt      = None
      PlatformIds      = Map.empty
      PreviousEpisodeId = None
      GeneratedAt      = None
      CreatedAt        = now }

// ── getDraft ─────────────────────────────────────────────────────────────────

[<Fact>]
let ``getDraft returns Draft episode when one exists`` () =
    task {
        let ep = makeEp Draft
        let repo = FakeEpisodeRepository(seed = [ep])
        let! result = getDraft repo
        result |> should not' (equal None)
        result.Value.Status |> should equal Draft
    }

[<Fact>]
let ``getDraft returns None when no Draft exists`` () =
    task {
        let repo = FakeEpisodeRepository(seed = [makeEp Published; makeEp Queued])
        let! result = getDraft repo
        result |> should equal None
    }

// ── getEpisode ────────────────────────────────────────────────────────────────

[<Fact>]
let ``getEpisode returns Ok with episode when found`` () =
    task {
        let ep = makeEp Queued
        let repo = FakeEpisodeRepository(seed = [ep])
        let! result = getEpisode repo ep.Id
        match result with
        | Ok found -> found.Id |> should equal ep.Id
        | Error e  -> failwith $"Unexpected error: {e}"
    }

[<Fact>]
let ``getEpisode returns NotFound for unknown id`` () =
    task {
        let repo = FakeEpisodeRepository()
        let! result = getEpisode repo (EpisodeId(Guid.NewGuid()))
        match result with
        | Error (NotFound _) -> ()
        | _ -> failwith "Expected NotFound"
    }

// ── listEpisodes ──────────────────────────────────────────────────────────────

[<Fact>]
let ``listEpisodes returns all episodes when no filter`` () =
    task {
        let repo = FakeEpisodeRepository(seed = [makeEp Queued; makeEp Draft; makeEp Published])
        let! result = listEpisodes repo None
        result |> List.length |> should equal 3
    }

[<Fact>]
let ``listEpisodes filters by status when provided`` () =
    task {
        let repo = FakeEpisodeRepository(seed = [makeEp Queued; makeEp Draft; makeEp Published])
        let! result = listEpisodes repo (Some Draft)
        result |> List.length |> should equal 1
        result |> List.head |> (fun ep -> ep.Status) |> should equal Draft
    }

[<Fact>]
let ``listEpisodes returns empty list when no episodes match filter`` () =
    task {
        let repo = FakeEpisodeRepository(seed = [makeEp Queued; makeEp Draft])
        let! result = listEpisodes repo (Some Failed)
        result |> List.isEmpty |> should equal true
    }

module DevPulse.Application.Tests.Generation.GenerateEpisodeCommandTests

open System
open Xunit
open FsUnit.Xunit
open DevPulse.Domain.Episodes
open DevPulse.Application.Episodes
open DevPulse.Application.Generation.GenerateEpisodeCommands
open DevPulse.Application.Tests.Episodes.FakeEpisodeRepository
open DevPulse.Application.Tests.Generation.FakeClaudeClient

// ── Helpers ───────────────────────────────────────────────────────────────────

let private now = DateTimeOffset.UtcNow

let private makeEpisode () : Episode =
    { Id               = EpisodeId(Guid.NewGuid())
      Concept          = "The Maybe Monad"
      Tag              = "functional"
      Language         = Some "fsharp"
      EpisodeNumber    = 1
      Status           = Queued
      Content          = None
      WasEdited        = false
      PublishAt        = Some (now.AddHours 1)
      PublishedAt      = None
      PlatformIds      = Map.empty
      PreviousEpisodeId = None
      GeneratedAt      = None
      CreatedAt        = now }

let private makeOutput () : EpisodeOutput =
    { Article = {
        Title           = "The Maybe Monad"
        Subtitle        = "You already use this"
        RealWorldAnchor = "user?.email"
        Body            = "Body text"
        RunnableSnippet = None
        ImagePrompt     = "An abstract image"
        CoverImageUrl   = None
        Foreshadow      = "Tomorrow: Result<T>"
        Tags            = ["functional"; "fsharp"] }
      Reddit  = None
      YouTube = None }

// ── Tests ─────────────────────────────────────────────────────────────────────

[<Fact>]
let ``returns NotFound when episode does not exist`` () =
    task {
        let repo   = FakeEpisodeRepository()
        let claude = FakeClaudeClient(Ok(makeOutput()))
        let cmd    = { EpisodeId = EpisodeId(Guid.NewGuid()); Now = now }
        let! result = generateEpisode repo claude cmd
        match result with
        | Error (NotFound _) -> ()
        | other -> failwith $"Expected NotFound but got {other}"
    }

[<Fact>]
let ``returns DomainError when episode is not Queued`` () =
    task {
        let ep     = { makeEpisode() with Status = Draft }
        let repo   = FakeEpisodeRepository(seed = [ep])
        let claude = FakeClaudeClient(Ok(makeOutput()))
        let cmd    = { EpisodeId = ep.Id; Now = now }
        let! result = generateEpisode repo claude cmd
        match result with
        | Error (DomainError _) -> ()
        | other -> failwith $"Expected DomainError but got {other}"
    }

[<Fact>]
let ``on Claude success, episode transitions to Draft with content`` () =
    task {
        let ep     = makeEpisode()
        let repo   = FakeEpisodeRepository(seed = [ep])
        let claude = FakeClaudeClient(Ok(makeOutput()))
        let cmd    = { EpisodeId = ep.Id; Now = now }
        let! result = generateEpisode repo claude cmd
        match result with
        | Ok draft ->
            draft.Status  |> should equal Draft
            draft.Content |> should not' (equal None)
        | Error e -> failwith $"Expected Ok but got {e}"
    }

[<Fact>]
let ``on Claude failure, episode transitions to Failed`` () =
    task {
        let ep     = makeEpisode()
        let repo   = FakeEpisodeRepository(seed = [ep])
        let claude = FakeClaudeClient(Error "API timeout")
        let cmd    = { EpisodeId = ep.Id; Now = now }
        let! result = generateEpisode repo claude cmd
        match result with
        | Error (DomainError _) ->
            repo.Store
            |> List.find (fun e -> e.Id = ep.Id)
            |> fun e -> e.Status |> should equal Failed
        | other -> failwith $"Expected DomainError but got {other}"
    }

[<Fact>]
let ``recent published concepts are passed to Claude`` () =
    task {
        let published = { makeEpisode() with Status = Published; Concept = "Currying" }
        let ep        = makeEpisode()
        let repo      = FakeEpisodeRepository(seed = [published; ep])
        let claude    = FakeClaudeClient(Ok(makeOutput()))
        let cmd       = { EpisodeId = ep.Id; Now = now }
        let! _        = generateEpisode repo claude cmd
        claude.CapturedInput
        |> Option.get
        |> fun i -> i.RecentConcepts |> should contain "Currying"
    }

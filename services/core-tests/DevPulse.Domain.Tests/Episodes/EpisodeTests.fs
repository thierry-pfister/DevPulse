module DevPulse.Domain.Tests.Episodes.EpisodeTests

open System
open Xunit
open FsUnit.Xunit
open DevPulse.Domain.Episodes

// ── Helpers ──────────────────────────────────────────────────────────────────

let private now = DateTimeOffset.UtcNow

let private freshEpisode () =
    Episode.create
        (EpisodeId(Guid.NewGuid()))
        "The Maybe Monad"
        "functional"
        (Some "fsharp")
        1
        (now.AddHours(1))
        now

let private episodeInStatus status =
    { freshEpisode () with Status = status }

let private assertOkStatus expected result =
    match result with
    | Ok ep -> ep.Status |> should equal expected
    | Error e -> failwith $"Expected Ok but got Error: {e}"

let private assertError result =
    match result with
    | Error _ -> ()
    | Ok _ -> failwith "Expected Error but got Ok"

// ── create ────────────────────────────────────────────────────────────────────

[<Fact>]
let ``create sets status to Queued`` () =
    (freshEpisode ()).Status |> should equal Queued

[<Fact>]
let ``create preserves concept and tag`` () =
    let ep = freshEpisode ()
    ep.Concept |> should equal "The Maybe Monad"
    ep.Tag     |> should equal "functional"

[<Fact>]
let ``create sets content to None`` () =
    (freshEpisode ()).Content |> should equal None

[<Fact>]
let ``create sets wasEdited to false`` () =
    (freshEpisode ()).WasEdited |> should equal false

[<Fact>]
let ``create sets platformIds to empty`` () =
    (freshEpisode ()).PlatformIds |> Map.isEmpty |> should equal true

// ── startGeneration ───────────────────────────────────────────────────────────

[<Fact>]
let ``startGeneration transitions Queued to Generating`` () =
    freshEpisode () |> Episode.startGeneration |> assertOkStatus Generating

[<Theory>]
[<InlineData("Generating")>]
[<InlineData("Draft")>]
[<InlineData("Published")>]
[<InlineData("Skipped")>]
[<InlineData("Failed")>]
let ``startGeneration returns error for non-Queued status`` (statusName: string) =
    let status =
        match statusName with
        | "Generating" -> Generating | "Draft" -> Draft
        | "Published"  -> Published  | "Skipped" -> Skipped
        | _            -> Failed
    episodeInStatus status |> Episode.startGeneration |> assertError

// ── complete ─────────────────────────────────────────────────────────────────

let private dummyContent : EpisodeOutput =
    { Article =
        { Title           = "title"
          Subtitle        = "subtitle"
          RealWorldAnchor = "anchor"
          Body            = "body"
          RunnableSnippet = None
          ImagePrompt     = "imagePrompt"
          CoverImageUrl   = None
          Foreshadow      = "foreshadow"
          Tags            = [] }
      Reddit  = None
      YouTube = None }

[<Fact>]
let ``complete transitions Generating to Draft with content`` () =
    let episode = episodeInStatus Generating
    match Episode.complete dummyContent now episode with
    | Ok ep ->
        ep.Status       |> should equal Draft
        ep.Content      |> should not' (equal None)
        ep.GeneratedAt  |> should not' (equal None)
    | Error e -> failwith e

[<Theory>]
[<InlineData("Queued")>]
[<InlineData("Draft")>]
[<InlineData("Published")>]
[<InlineData("Skipped")>]
[<InlineData("Failed")>]
let ``complete returns error for non-Generating status`` (statusName: string) =
    let status =
        match statusName with
        | "Queued"    -> Queued    | "Draft"    -> Draft
        | "Published" -> Published | "Skipped"  -> Skipped
        | _           -> Failed
    episodeInStatus status |> Episode.complete dummyContent now |> assertError

// ── fail ──────────────────────────────────────────────────────────────────────

[<Fact>]
let ``fail transitions Generating to Failed`` () =
    episodeInStatus Generating |> Episode.fail |> assertOkStatus Failed

[<Theory>]
[<InlineData("Queued")>]
[<InlineData("Draft")>]
[<InlineData("Published")>]
[<InlineData("Skipped")>]
let ``fail returns error for non-Generating status`` (statusName: string) =
    let status =
        match statusName with
        | "Queued" -> Queued | "Draft" -> Draft
        | "Published" -> Published | _ -> Skipped
    episodeInStatus status |> Episode.fail |> assertError

// ── approve ───────────────────────────────────────────────────────────────────

[<Fact>]
let ``approve transitions Draft to Published and sets publishedAt`` () =
    let episode = episodeInStatus Draft
    match Episode.approve now episode with
    | Ok ep ->
        ep.Status      |> should equal Published
        ep.PublishedAt |> should not' (equal None)
    | Error e -> failwith e

[<Theory>]
[<InlineData("Queued")>]
[<InlineData("Generating")>]
[<InlineData("Published")>]
[<InlineData("Skipped")>]
[<InlineData("Failed")>]
let ``approve returns error for non-Draft status`` (statusName: string) =
    let status =
        match statusName with
        | "Queued"     -> Queued     | "Generating" -> Generating
        | "Published"  -> Published  | "Skipped"    -> Skipped
        | _            -> Failed
    episodeInStatus status |> Episode.approve now |> assertError

// ── skip ─────────────────────────────────────────────────────────────────────

[<Fact>]
let ``skip transitions Draft to Skipped`` () =
    episodeInStatus Draft |> Episode.skip |> assertOkStatus Skipped

[<Theory>]
[<InlineData("Queued")>]
[<InlineData("Generating")>]
[<InlineData("Published")>]
[<InlineData("Failed")>]
let ``skip returns error for non-Draft status`` (statusName: string) =
    let status =
        match statusName with
        | "Queued" -> Queued | "Generating" -> Generating
        | "Published" -> Published | _ -> Failed
    episodeInStatus status |> Episode.skip |> assertError

// ── delay ─────────────────────────────────────────────────────────────────────

[<Fact>]
let ``delay updates publishAt on Draft episode`` () =
    let newTime = now.AddHours(2)
    let episode = episodeInStatus Draft
    match Episode.delay newTime episode with
    | Ok ep -> ep.PublishAt |> should equal (Some newTime)
    | Error e -> failwith e

[<Theory>]
[<InlineData("Queued")>]
[<InlineData("Generating")>]
[<InlineData("Published")>]
[<InlineData("Skipped")>]
[<InlineData("Failed")>]
let ``delay returns error for non-Draft status`` (statusName: string) =
    let status =
        match statusName with
        | "Queued"    -> Queued    | "Generating" -> Generating
        | "Published" -> Published | "Skipped"    -> Skipped
        | _           -> Failed
    episodeInStatus status |> Episode.delay (now.AddHours(2)) |> assertError

// ── recordPlatformId ─────────────────────────────────────────────────────────

[<Fact>]
let ``recordPlatformId stores platform id in map`` () =
    let ep = freshEpisode () |> Episode.recordPlatformId "pfstr-core" "some-guid"
    ep.PlatformIds |> Map.find "pfstr-core" |> should equal "some-guid"

[<Fact>]
let ``recordPlatformId can store multiple platforms`` () =
    let ep =
        freshEpisode ()
        |> Episode.recordPlatformId "pfstr-core" "id-1"
        |> Episode.recordPlatformId "dev.to" "42"
    ep.PlatformIds |> Map.count |> should equal 2

// ── markEdited ────────────────────────────────────────────────────────────────

[<Fact>]
let ``markEdited sets wasEdited to true`` () =
    freshEpisode () |> Episode.markEdited |> (fun ep -> ep.WasEdited) |> should equal true

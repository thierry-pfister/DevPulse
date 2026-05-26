module DevPulse.Application.Tests.Generation.FakeClaudeClient

open System.Threading.Tasks
open DevPulse.Domain.Episodes
open DevPulse.Application.Generation

type FakeClaudeClient(result: Result<EpisodeOutput, string>) =
    let mutable capturedInput : GenerationInput option = None

    member _.CapturedInput = capturedInput

    interface IClaudeClient with
        member _.Generate input =
            capturedInput <- Some input
            Task.FromResult result

        member _.SuggestTopicsAsync _ _ _ =
            Task.FromResult([] : string list)

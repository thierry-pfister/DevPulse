module DevPulse.Application.Tests.Episodes.FakeEpisodeRepository

open System.Threading.Tasks
open DevPulse.Domain.Episodes
open DevPulse.Application.Episodes

type FakeEpisodeRepository(?seed: Episode list) =
    let mutable store : Map<EpisodeId, Episode> =
        seed
        |> Option.defaultValue []
        |> List.map (fun ep -> ep.Id, ep)
        |> Map.ofList
    let mutable counter = store.Count

    member _.Store = store |> Map.toList |> List.map snd

    interface IEpisodeRepository with
        member _.FindById id =
            Task.FromResult(store |> Map.tryFind id)

        member _.FindDraft() =
            store |> Map.tryFindKey (fun _ ep -> ep.Status = Draft)
            |> Option.map (fun k -> store[k])
            |> Task.FromResult

        member _.FindAll statusFilter =
            store
            |> Map.toList
            |> List.map snd
            |> List.filter (fun ep ->
                match statusFilter with
                | None   -> true
                | Some s -> ep.Status = s)
            |> Task.FromResult

        member _.Save ep =
            store <- Map.add ep.Id ep store
            Task.CompletedTask

        member _.NextNumber() =
            counter <- counter + 1
            Task.FromResult counter

module DevPulse.Application.Episodes.EpisodeQueries

open System.Threading.Tasks
open DevPulse.Domain.Episodes

let getDraft
        (repo: IEpisodeRepository)
        : Task<Episode option> =
    repo.FindDraft()

let getEpisode
        (repo: IEpisodeRepository)
        (id:   EpisodeId)
        : Task<Result<Episode, EpisodeApplicationError>> =
    task {
        let! found = repo.FindById id
        return
            match found with
            | Some ep -> Ok ep
            | None    -> Error (NotFound $"Episode {id} not found")
    }

let listEpisodes
        (repo:   IEpisodeRepository)
        (status: EpisodeStatus option)
        : Task<Episode list> =
    repo.FindAll status

namespace DevPulse.Application.Episodes

open System.Threading.Tasks
open DevPulse.Domain.Episodes

type EpisodeApplicationError =
    | ValidationError of string
    | NotFound        of string
    | Conflict        of string
    | DomainError     of string

type IEpisodeRepository =
    abstract member FindById   : EpisodeId -> Task<Episode option>
    abstract member FindDraft  : unit -> Task<Episode option>
    abstract member FindAll    : EpisodeStatus option -> Task<Episode list>
    abstract member Save       : Episode -> Task
    abstract member NextNumber : unit -> Task<int>

namespace DevPulse.Application.Publishing

open DevPulse.Domain.Episodes
open System.Threading.Tasks

type PublishResult =
    | Published of platformId: string
    | Skipped   of reason: string
    | Failed    of error: string

type IPublisher =
    abstract member Name         : string
    abstract member PublishAsync : EpisodeOutput -> Task<PublishResult>

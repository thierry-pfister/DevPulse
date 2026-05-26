namespace DevPulse.Application.Generation

open System.Threading.Tasks
open DevPulse.Domain.Episodes

type IClaudeClient =
    abstract member Generate         : GenerationInput -> Task<Result<EpisodeOutput, string>>
    abstract member SuggestTopicsAsync : tag: string -> count: int -> existing: string list -> Task<string list>

namespace DevPulse.Domain.Episodes

type GenerationInput = {
    Concept: string
    Tag: string
    Language: string option
    EpisodeNumber: int
    RealWorldAnchorHint: string option
    Runnable: bool
    ForeshadowTopic: string option
    Tone: string
    TargetAudience: string
    RecentConcepts: string list
}

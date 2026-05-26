namespace DevPulse.Domain.Episodes

type ArticleContent = {
    Title: string
    Subtitle: string
    RealWorldAnchor: string
    Body: string
    RunnableSnippet: string option
    ImagePrompt: string
    CoverImageUrl: string option
    Foreshadow: string
    Tags: string list
}

type RedditContent = {
    Title: string
    Body: string
}

type YouTubeContent = {
    Title: string
    Description: string
    Script: string
}

type EpisodeOutput = {
    Article: ArticleContent
    Reddit: RedditContent option
    YouTube: YouTubeContent option
}

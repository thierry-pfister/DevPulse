export type EpisodeStatus =
  | 'Queued'
  | 'Generating'
  | 'Draft'
  | 'Published'
  | 'Skipped'
  | 'Failed'

export interface ArticleContent {
  title: string
  subtitle: string
  realWorldAnchor: string
  body: string
  runnableSnippet: string | null
  imagePrompt: string
  foreshadow: string
  tags: string[]
}

export interface Episode {
  id: string
  concept: string
  tag: string
  language: string | null
  episodeNumber: number
  status: EpisodeStatus
  wasEdited: boolean
  publishAt: string | null
  publishedAt: string | null
  generatedAt: string | null
  createdAt: string
  platformIds: Record<string, string>
  content: ArticleContent | null
}

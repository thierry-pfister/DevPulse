import { getEpisode } from '@/lib/api'
import Link from 'next/link'
import Markdown from 'react-markdown'

export default async function EpisodeDetailPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = await params
  const episode = await getEpisode(id)
  const { content } = episode

  const publishDate = episode.publishAt ? new Date(episode.publishAt) : null
  const publishedDate = episode.publishedAt ? new Date(episode.publishedAt) : null

  return (
    <div className="space-y-6">
      <div>
        <Link href="/episodes" className="text-sm text-slate-500 hover:text-slate-700">
          ← Episodes
        </Link>
      </div>

      <div className="flex flex-col gap-1">
        <div className="flex items-center gap-2 text-sm text-slate-500">
          <span className="font-mono">#{episode.episodeNumber}</span>
          <span>·</span>
          <span className="capitalize">{episode.tag}</span>
          {episode.language && <><span>·</span><span className="font-mono">{episode.language}</span></>}
        </div>
        <h1 className="text-2xl font-bold text-slate-900">{episode.concept}</h1>
        {content && <p className="text-slate-600 text-lg">{content.subtitle}</p>}
      </div>

      {/* Metadata */}
      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm grid grid-cols-2 gap-3 text-sm">
        <div>
          <span className="text-slate-400 text-xs font-semibold uppercase tracking-widest">Status</span>
          <p className="mt-1 font-medium">{episode.status}</p>
        </div>
        <div>
          <span className="text-slate-400 text-xs font-semibold uppercase tracking-widest">Episode</span>
          <p className="mt-1 font-mono">#{episode.episodeNumber}</p>
        </div>
        {publishDate && (
          <div>
            <span className="text-slate-400 text-xs font-semibold uppercase tracking-widest">Scheduled</span>
            <p className="mt-1 font-mono text-xs">{publishDate.toLocaleString()}</p>
          </div>
        )}
        {publishedDate && (
          <div>
            <span className="text-slate-400 text-xs font-semibold uppercase tracking-widest">Published</span>
            <p className="mt-1 font-mono text-xs">{publishedDate.toLocaleString()}</p>
          </div>
        )}
        {Object.keys(episode.platformIds).length > 0 && (
          <div className="col-span-2">
            <span className="text-slate-400 text-xs font-semibold uppercase tracking-widest">Platform IDs</span>
            <div className="mt-1 flex flex-wrap gap-2">
              {Object.entries(episode.platformIds).map(([k, v]) => (
                <span key={k} className="font-mono text-xs bg-slate-100 px-2 py-0.5 rounded">
                  {k}: {v}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>

      {content ? (
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm space-y-6">
          <div>
            <p className="text-xs font-semibold uppercase tracking-widest text-slate-400 mb-2">Real-world anchor</p>
            <p className="text-slate-700 italic">{content.realWorldAnchor}</p>
          </div>
          <div>
            <p className="text-xs font-semibold uppercase tracking-widest text-slate-400 mb-3">Article</p>
            <article className="prose prose-slate max-w-none">
              <Markdown>{content.body}</Markdown>
            </article>
          </div>
          {content.runnableSnippet && (
            <div>
              <p className="text-xs font-semibold uppercase tracking-widest text-slate-400 mb-2">Runnable snippet</p>
              <pre className="bg-slate-900 text-slate-100 rounded-lg p-4 text-sm overflow-x-auto">
                <code>{content.runnableSnippet}</code>
              </pre>
            </div>
          )}
          <div>
            <p className="text-xs font-semibold uppercase tracking-widest text-slate-400 mb-2">Foreshadow</p>
            <p className="text-slate-600 border-l-4 border-slate-200 pl-3">{content.foreshadow}</p>
          </div>
          <div className="flex flex-wrap gap-2 pt-2 border-t border-slate-100">
            {content.tags.map(tag => (
              <span key={tag} className="px-2 py-1 rounded-md bg-slate-100 text-slate-600 text-xs font-mono">
                {tag}
              </span>
            ))}
          </div>
        </div>
      ) : (
        <div className="rounded-xl border border-dashed border-slate-300 p-8 text-center text-slate-400">
          No content available
        </div>
      )}
    </div>
  )
}

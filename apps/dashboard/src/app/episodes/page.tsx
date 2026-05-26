import { listEpisodes } from '@/lib/api'
import type { EpisodeStatus } from '@/types/episode'
import Link from 'next/link'

const STATUS_STYLES: Record<EpisodeStatus, string> = {
  Queued:     'bg-slate-100 text-slate-600',
  Generating: 'bg-blue-100 text-blue-700',
  Draft:      'bg-amber-100 text-amber-700',
  Published:  'bg-emerald-100 text-emerald-700',
  Skipped:    'bg-slate-100 text-slate-500',
  Failed:     'bg-red-100 text-red-700',
}

export default async function EpisodesPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string }>
}) {
  const { status } = await searchParams
  const episodes = await listEpisodes(status as EpisodeStatus | undefined)

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-bold text-slate-900">Episodes</h1>
        <div className="flex gap-2 text-sm">
          {(['', 'Draft', 'Published', 'Queued', 'Failed'] as const).map((s) => (
            <Link
              key={s}
              href={s ? `/episodes?status=${s}` : '/episodes'}
              className={`px-3 py-1 rounded-full border transition-colors ${
                (status ?? '') === s
                  ? 'bg-slate-900 text-white border-slate-900'
                  : 'border-slate-200 text-slate-600 hover:border-slate-400'
              }`}
            >
              {s || 'All'}
            </Link>
          ))}
        </div>
      </div>

      {episodes.length === 0 ? (
        <p className="text-slate-400 py-12 text-center">No episodes found</p>
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="text-left px-4 py-3 font-semibold text-slate-500 w-10">#</th>
                <th className="text-left px-4 py-3 font-semibold text-slate-500">Concept</th>
                <th className="text-left px-4 py-3 font-semibold text-slate-500">Tag</th>
                <th className="text-left px-4 py-3 font-semibold text-slate-500">Status</th>
                <th className="text-left px-4 py-3 font-semibold text-slate-500">Publish at</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {episodes.map((ep) => (
                <tr key={ep.id} className="hover:bg-slate-50 transition-colors">
                  <td className="px-4 py-3 font-mono text-slate-400">{ep.episodeNumber}</td>
                  <td className="px-4 py-3">
                    <Link
                      href={`/episodes/${ep.id}`}
                      className="font-medium text-slate-900 hover:text-slate-600 hover:underline"
                    >
                      {ep.concept}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-slate-500 capitalize">{ep.tag}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_STYLES[ep.status]}`}>
                      {ep.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-slate-500 font-mono text-xs">
                    {ep.publishAt ? formatDate(ep.publishAt) : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

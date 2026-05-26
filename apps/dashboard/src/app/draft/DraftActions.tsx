'use client'

import { useState } from 'react'
import { approveEpisode, skipEpisode, delayEpisode } from './actions'

interface Props {
  id: string
  publishAt: string | null
}

export function DraftActions({ id, publishAt }: Props) {
  const [showDelay, setShowDelay] = useState(false)
  const [pending, setPending] = useState(false)

  const defaultDate = publishAt
    ? new Date(publishAt).toISOString().slice(0, 16)
    : new Date(Date.now() + 3600_000).toISOString().slice(0, 16)

  return (
    <div className="flex flex-wrap items-center gap-3">
      <form action={approveEpisode.bind(null, id)} onSubmit={() => setPending(true)}>
        <button
          type="submit"
          disabled={pending}
          className="px-5 py-2 rounded-lg bg-emerald-600 text-white font-medium text-sm hover:bg-emerald-700 disabled:opacity-50 transition-colors"
        >
          ✓ Approve
        </button>
      </form>

      {showDelay ? (
        <form
          action={delayEpisode.bind(null, id)}
          onSubmit={() => { setPending(true); setShowDelay(false) }}
          className="flex items-center gap-2"
        >
          <input
            type="datetime-local"
            name="newPublishAt"
            defaultValue={defaultDate}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500"
          />
          <button
            type="submit"
            className="px-4 py-2 rounded-lg bg-amber-500 text-white font-medium text-sm hover:bg-amber-600 transition-colors"
          >
            Set
          </button>
          <button
            type="button"
            onClick={() => setShowDelay(false)}
            className="px-3 py-2 rounded-lg text-sm text-slate-600 hover:text-slate-900 transition-colors"
          >
            Cancel
          </button>
        </form>
      ) : (
        <button
          onClick={() => setShowDelay(true)}
          className="px-5 py-2 rounded-lg bg-amber-500 text-white font-medium text-sm hover:bg-amber-600 transition-colors"
        >
          ⏸ Delay
        </button>
      )}

      <form action={skipEpisode.bind(null, id)} onSubmit={() => setPending(true)}>
        <button
          type="submit"
          disabled={pending}
          className="px-5 py-2 rounded-lg bg-red-600 text-white font-medium text-sm hover:bg-red-700 disabled:opacity-50 transition-colors"
        >
          ✕ Skip
        </button>
      </form>
    </div>
  )
}

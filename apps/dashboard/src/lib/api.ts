import type { Episode, EpisodeStatus } from '@/types/episode'

const API_BASE = process.env.API_BASE_URL ?? 'http://localhost:5013'
const API_KEY = process.env.API_KEY ?? ''

const authHeaders = {
  'X-Api-Key': API_KEY,
  'Content-Type': 'application/json',
}

export async function getDraft(): Promise<Episode | null> {
  const res = await fetch(`${API_BASE}/api/episodes/draft`, {
    headers: authHeaders,
    cache: 'no-store',
  })
  if (res.status === 404) return null
  if (!res.ok) throw new Error(`GET /api/episodes/draft → ${res.status}`)
  return res.json()
}

export async function listEpisodes(status?: EpisodeStatus): Promise<Episode[]> {
  const url = status
    ? `${API_BASE}/api/episodes?status=${status}`
    : `${API_BASE}/api/episodes`
  const res = await fetch(url, {
    headers: authHeaders,
    cache: 'no-store',
  })
  if (!res.ok) throw new Error(`GET /api/episodes → ${res.status}`)
  return res.json()
}

export async function getEpisode(id: string): Promise<Episode> {
  const res = await fetch(`${API_BASE}/api/episodes/${id}`, {
    headers: authHeaders,
    cache: 'no-store',
  })
  if (!res.ok) throw new Error(`GET /api/episodes/${id} → ${res.status}`)
  return res.json()
}

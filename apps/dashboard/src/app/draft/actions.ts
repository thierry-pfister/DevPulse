'use server'

import { revalidatePath } from 'next/cache'

const API_BASE = process.env.API_BASE_URL ?? 'http://localhost:5013'
const API_KEY = process.env.API_KEY ?? ''

const headers = {
  'X-Api-Key': API_KEY,
  'Content-Type': 'application/json',
}

export async function approveEpisode(id: string) {
  await fetch(`${API_BASE}/api/episodes/${id}/approve`, { method: 'POST', headers })
  revalidatePath('/draft')
  revalidatePath('/episodes')
}

export async function skipEpisode(id: string) {
  await fetch(`${API_BASE}/api/episodes/${id}/skip`, { method: 'POST', headers })
  revalidatePath('/draft')
  revalidatePath('/episodes')
}

export async function delayEpisode(id: string, formData: FormData) {
  const raw = formData.get('newPublishAt') as string
  const newPublishAt = new Date(raw).toISOString()
  await fetch(`${API_BASE}/api/episodes/${id}/delay`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ newPublishAt }),
  })
  revalidatePath('/draft')
}

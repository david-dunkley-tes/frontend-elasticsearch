import type { SearchRequest, SearchResponse } from '../types';

const API_BASE = 'http://localhost:5000';

export async function searchStudents(request: SearchRequest, signal: AbortSignal) {
  const response = await fetch(`${API_BASE}/api/search`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SearchResponse>;
}

export async function reindexStudents() {
  const response = await fetch(`${API_BASE}/api/admin/reindex`, { method: 'POST' });

  if (!response.ok) {
    throw new Error(await response.text());
  }
}

import type { SavedSearch, SaveSearchRequest, SearchRequest, SearchResponse } from '../types';

const API_BASE = `${window.location.protocol}//${window.location.hostname}:5000`;

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

export async function listSavedSearches() {
  const response = await fetch(`${API_BASE}/api/saved-searches`);

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SavedSearch[]>;
}

export async function saveSearch(request: SaveSearchRequest) {
  const response = await fetch(`${API_BASE}/api/saved-searches`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SavedSearch>;
}

export async function deleteSavedSearch(id: string) {
  const response = await fetch(`${API_BASE}/api/saved-searches/${id}`, { method: 'DELETE' });

  if (!response.ok) {
    throw new Error(await response.text());
  }
}

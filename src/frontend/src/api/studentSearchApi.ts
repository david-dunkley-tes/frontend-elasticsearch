import type { CurrentUser, SavedSearch, SaveSearchRequest, SearchRequest, SearchResponse } from '../types';

const API_BASE = `${window.location.protocol}//${window.location.hostname}:5000`;
const DEV_ACCESS_TOKEN = encodeDevAccessToken({
  sub: 'dev-kingfisher-academy',
  name: 'Kingfisher Academy',
  scopes: [{ type: 'school', schoolId: 'SCH-KINGFISHER' }],
});

const authHeaders = {
  Authorization: `Bearer ${DEV_ACCESS_TOKEN}`,
};

export async function searchStudents(request: SearchRequest, signal: AbortSignal) {
  const response = await fetch(`${API_BASE}/api/search`, {
    method: 'POST',
    headers: { ...authHeaders, 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SearchResponse>;
}

export async function reindexStudents() {
  const response = await fetch(`${API_BASE}/api/admin/reindex`, { method: 'POST', headers: authHeaders });

  if (!response.ok) {
    throw new Error(await response.text());
  }
}

export async function listSavedSearches() {
  const response = await fetch(`${API_BASE}/api/saved-searches`, { headers: authHeaders });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SavedSearch[]>;
}

export async function saveSearch(request: SaveSearchRequest) {
  const response = await fetch(`${API_BASE}/api/saved-searches`, {
    method: 'POST',
    headers: { ...authHeaders, 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SavedSearch>;
}

export async function deleteSavedSearch(id: string) {
  const response = await fetch(`${API_BASE}/api/saved-searches/${id}`, { method: 'DELETE', headers: authHeaders });

  if (!response.ok) {
    throw new Error(await response.text());
  }
}

export async function getCurrentUser() {
  const response = await fetch(`${API_BASE}/api/auth/me`, { headers: authHeaders });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<CurrentUser>;
}

function encodeDevAccessToken(payload: { sub: string; name: string; scopes: Array<Record<string, string>> }) {
  const json = JSON.stringify(payload);
  const bytes = new TextEncoder().encode(json);
  let binary = '';
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });

  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

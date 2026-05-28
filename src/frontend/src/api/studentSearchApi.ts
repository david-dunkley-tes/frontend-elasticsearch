import { DEFAULT_PRESET_ID, findPreset, type UserPresetToken } from '../auth/userPresets';
import type { RagAnswer, RagHealth, SavedSearch, SaveSearchRequest, SearchRequest, SearchResponse, VersionInfo } from '../types';

const API_BASE = import.meta.env.VITE_API_BASE ?? `${window.location.protocol}//${window.location.hostname}:5000`;

let activeDevToken = encodeDevAccessToken(findPreset(DEFAULT_PRESET_ID)!.token);

export function setActiveDevToken(payload: UserPresetToken) {
  activeDevToken = encodeDevAccessToken(payload);
}

function authHeaders(): HeadersInit {
  return { Authorization: `Bearer ${activeDevToken}` };
}

export async function searchStudents(request: SearchRequest, signal: AbortSignal) {
  const response = await fetch(`${API_BASE}/api/search`, {
    method: 'POST',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal,
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SearchResponse>;
}

export async function reindexStudents() {
  const response = await fetch(`${API_BASE}/api/admin/reindex`, { method: 'POST', headers: authHeaders() });

  if (!response.ok) {
    throw new Error(await response.text());
  }
}

export async function listSavedSearches() {
  const response = await fetch(`${API_BASE}/api/saved-searches`, { headers: authHeaders() });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SavedSearch[]>;
}

export async function saveSearch(request: SaveSearchRequest) {
  const response = await fetch(`${API_BASE}/api/saved-searches`, {
    method: 'POST',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<SavedSearch>;
}

export async function deleteSavedSearch(id: string) {
  const response = await fetch(`${API_BASE}/api/saved-searches/${id}`, { method: 'DELETE', headers: authHeaders() });

  if (!response.ok) {
    throw new Error(await response.text());
  }
}

export async function askStudents(question: string, debugMode: boolean) {
  const response = await fetch(`${API_BASE}/api/ask`, {
    method: 'POST',
    headers: { ...authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({ question, debugMode }),
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<RagAnswer>;
}

export async function getAskHealth() {
  const response = await fetch(`${API_BASE}/api/ask/health`, { headers: authHeaders() });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<RagHealth>;
}

export async function getVersionInfo() {
  const response = await fetch(`${API_BASE}/version`);

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json() as Promise<VersionInfo>;
}

function encodeDevAccessToken(payload: UserPresetToken) {
  const json = JSON.stringify(payload);
  const bytes = new TextEncoder().encode(json);
  let binary = '';
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });

  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

import React from 'react';
import { createRoot } from 'react-dom/client';
import { AlertTriangle, Bug, ChevronLeft, ChevronRight, Database, RefreshCw, Search, X } from 'lucide-react';
import './styles.css';

const API_BASE = 'http://localhost:5000';

type Student = {
  id: string;
  foreName: string;
  surname: string;
  fullName: string;
  yearGroup: string;
};

type School = {
  name: string;
  address: string;
};

type Trust = {
  name: string;
} | null;

type SearchResult = {
  id: string;
  student: Student;
  school: School;
  trust: Trust;
  highlights: Record<string, string[]>;
  score?: number;
};

type FacetOption = {
  value: string;
  label: string;
  count: number;
  selected: boolean;
};

type Facet = {
  label: string;
  type: string;
  selected: string[];
  options: FacetOption[];
};

type SearchResponse = {
  total: number;
  tookMs: number;
  backendTookMs: number;
  results: SearchResult[];
  facets: Record<string, Facet>;
  debug?: unknown;
};

type Filters = Record<string, string[]>;

function App() {
  const [query, setQuery] = React.useState('');
  const [filters, setFilters] = React.useState<Filters>({});
  const [page, setPage] = React.useState(1);
  const [debugMode, setDebugMode] = React.useState(true);
  const [response, setResponse] = React.useState<SearchResponse | null>(null);
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(false);
  const [reindexing, setReindexing] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const pageSize = 10;

  const requestPayload = React.useMemo(
    () => ({ query, filters, sort: query.trim() ? 'relevance' : 'studentName', page, pageSize, debugMode }),
    [query, filters, page, debugMode],
  );

  React.useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);

    fetch(`${API_BASE}/api/search`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestPayload),
      signal: controller.signal,
    })
      .then(async (res) => {
        if (!res.ok) {
          throw new Error(await res.text());
        }
        return res.json() as Promise<SearchResponse>;
      })
      .then((data) => {
        setResponse(data);
        setSelectedId((current) => current && data.results.some((result) => result.id === current) ? current : data.results[0]?.id ?? null);
      })
      .catch((err: Error) => {
        if (err.name !== 'AbortError') {
          setError(err.message);
          setResponse(null);
        }
      })
      .finally(() => setLoading(false));

    return () => controller.abort();
  }, [requestPayload]);

  const selectedResult = response?.results.find((result) => result.id === selectedId) ?? response?.results[0] ?? null;
  const pageCount = response ? Math.max(1, Math.ceil(response.total / pageSize)) : 1;

  function updateQuery(value: string) {
    setQuery(value);
    setPage(1);
  }

  function toggleFilter(facetId: string, value: string) {
    setFilters((current) => {
      const values = current[facetId] ?? [];
      const nextValues = values.includes(value) ? values.filter((item) => item !== value) : [...values, value];
      const next = { ...current, [facetId]: nextValues };
      if (nextValues.length === 0) {
        delete next[facetId];
      }
      return next;
    });
    setPage(1);
  }

  function clearFilter(facetId: string, value: string) {
    setFilters((current) => {
      const nextValues = (current[facetId] ?? []).filter((item) => item !== value);
      const next = { ...current, [facetId]: nextValues };
      if (nextValues.length === 0) {
        delete next[facetId];
      }
      return next;
    });
    setPage(1);
  }

  async function reindex() {
    setReindexing(true);
    setError(null);
    try {
      const res = await fetch(`${API_BASE}/api/admin/reindex`, { method: 'POST' });
      if (!res.ok) {
        throw new Error(await res.text());
      }
      setPage(1);
      setQuery((value) => value);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Reindex failed');
    } finally {
      setReindexing(false);
    }
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <h1>Student Search</h1>
          <p>Search students by name, ID, school, trust, year group, or address.</p>
        </div>
        <div className="topbar-actions">
          <button className="icon-button" onClick={reindex} disabled={reindexing} title="Reindex seed data">
            <Database size={18} />
            {reindexing ? 'Reindexing' : 'Reindex'}
          </button>
          <label className="debug-toggle">
            <input type="checkbox" checked={debugMode} onChange={(event) => setDebugMode(event.target.checked)} />
            <Bug size={16} />
            Debug
          </label>
        </div>
      </header>

      <section className="search-row">
        <Search size={20} />
        <input
          value={query}
          onChange={(event) => updateQuery(event.target.value)}
          placeholder="Search anything: student surname, ID, school, trust..."
          autoFocus
        />
        {query && (
          <button className="clear-button" onClick={() => updateQuery('')} title="Clear search">
            <X size={18} />
          </button>
        )}
      </section>

      <section className="selected-chips" aria-label="Selected filters">
        {Object.entries(filters).flatMap(([facetId, values]) =>
          values.map((value) => {
            const option = response?.facets[facetId]?.options.find((item) => item.value === value);
            return (
              <button key={`${facetId}:${value}`} onClick={() => clearFilter(facetId, value)}>
                {response?.facets[facetId]?.label ?? facetId}: {option?.label ?? value}
                <X size={14} />
              </button>
            );
          }),
        )}
      </section>

      <section className="workspace">
        <aside className="filters-panel">
          <h2>Filters</h2>
          {response && Object.entries(response.facets).map(([facetId, facet]) => (
            <FacetGroup key={facetId} facetId={facetId} facet={facet} onToggle={toggleFilter} />
          ))}
        </aside>

        <section className="results-panel">
          <div className="results-summary">
            <div>
              <strong>{loading ? 'Searching...' : `${response?.total ?? 0} students found`}</strong>
              <span>{query.trim() ? 'Sorted by relevance' : 'Sorted by surname, then forename'}</span>
            </div>
            {response && <span>{response.tookMs}ms Elasticsearch · {response.backendTookMs}ms backend</span>}
          </div>

          {error && (
            <div className="state-message error">
              <AlertTriangle size={18} />
              Unable to load results. Open Debug Mode for details.
            </div>
          )}

          {!error && !loading && response?.results.length === 0 && (
            <div className="state-message">No matching students found.</div>
          )}

          <div className="result-list">
            {response?.results.map((result) => (
              <button
                key={result.id}
                className={`result-card ${result.id === selectedResult?.id ? 'selected' : ''}`}
                onClick={() => setSelectedId(result.id)}
              >
                <div className="result-card-header">
                  <strong>{result.student.fullName}</strong>
                  <span>{result.student.yearGroup}</span>
                </div>
                <div className="muted">ID {result.student.id}</div>
                <div>{result.school.name}</div>
                <div className="muted">{result.school.address}</div>
                <div className="trust-line">{result.trust?.name ?? 'No trust'}</div>
                <Highlights highlights={result.highlights} />
              </button>
            ))}
          </div>

          <div className="pagination">
            <button disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>
              <ChevronLeft size={16} />
              Previous
            </button>
            <span>Page {page} of {pageCount}</span>
            <button disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}>
              Next
              <ChevronRight size={16} />
            </button>
          </div>
        </section>

        <aside className="detail-panel">
          <h2>Student detail</h2>
          {selectedResult ? <StudentDetail result={selectedResult} /> : <div className="state-message">Select a student.</div>}
        </aside>
      </section>

      {debugMode && (
        <section className="debug-panel">
          <div className="debug-header">
            <h2>Debug Mode</h2>
            <RefreshCw size={16} className={loading ? 'spin' : ''} />
          </div>
          {error && <pre>{error}</pre>}
          <pre>{JSON.stringify({ request: requestPayload, response }, null, 2)}</pre>
        </section>
      )}
    </main>
  );
}

function FacetGroup({ facetId, facet, onToggle }: { facetId: string; facet: Facet; onToggle: (facetId: string, value: string) => void }) {
  return (
    <section className="facet-group">
      <h3>{facet.label}</h3>
      <div className="facet-options">
        {facet.options.map((option) => (
          <label key={option.value} className={option.selected ? 'checked' : ''}>
            <input type="checkbox" checked={option.selected} onChange={() => onToggle(facetId, option.value)} />
            <span>{option.label}</span>
            <small>{option.count}</small>
          </label>
        ))}
      </div>
    </section>
  );
}

function Highlights({ highlights }: { highlights: Record<string, string[]> }) {
  const entries = Object.entries(highlights).slice(0, 2);
  if (entries.length === 0) {
    return null;
  }

  return (
    <div className="highlights">
      {entries.map(([field, snippets]) => (
        <div key={field}>
          <span>{field}</span>
          <p dangerouslySetInnerHTML={{ __html: snippets[0] }} />
        </div>
      ))}
    </div>
  );
}

function StudentDetail({ result }: { result: SearchResult }) {
  return (
    <div className="detail-content">
      <div>
        <span className="eyebrow">Student</span>
        <h3>{result.student.fullName}</h3>
        <dl>
          <dt>ID</dt>
          <dd>{result.student.id}</dd>
          <dt>Year group</dt>
          <dd>{result.student.yearGroup}</dd>
          <dt>Score</dt>
          <dd>{result.score?.toFixed(3) ?? 'N/A'}</dd>
        </dl>
      </div>
      <div>
        <span className="eyebrow">School</span>
        <h3>{result.school.name}</h3>
        <p>{result.school.address}</p>
      </div>
      <div>
        <span className="eyebrow">Trust</span>
        <h3>{result.trust?.name ?? 'No trust'}</h3>
      </div>
      <div>
        <span className="eyebrow">Matched fields</span>
        <Highlights highlights={result.highlights} />
      </div>
    </div>
  );
}

createRoot(document.getElementById('root')!).render(<App />);

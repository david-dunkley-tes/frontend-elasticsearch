import React from 'react';
import { deleteSavedSearch, listSavedSearches, reindexStudents, saveSearch, searchStudents } from './api/studentSearchApi';
import { DebugPanel } from './components/DebugPanel';
import { FacetGroup } from './components/FacetGroup';
import { ResultsPanel } from './components/ResultsPanel';
import { SearchBox } from './components/SearchBox';
import { SavedSearchesPanel } from './components/SavedSearchesPanel';
import { SelectedFilters } from './components/SelectedFilters';
import { StudentDetail } from './components/StudentDetail';
import { TopBar } from './components/TopBar';
import type { Filters, SavedSearch, SearchResponse } from './types';

export function App() {
  const [query, setQuery] = React.useState('');
  const [filters, setFilters] = React.useState<Filters>({});
  const [page, setPage] = React.useState(1);
  const [debugMode, setDebugMode] = React.useState(true);
  const [response, setResponse] = React.useState<SearchResponse | null>(null);
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [savedSearches, setSavedSearches] = React.useState<SavedSearch[]>([]);
  const [saveName, setSaveName] = React.useState('');
  const [loading, setLoading] = React.useState(false);
  const [savingSearch, setSavingSearch] = React.useState(false);
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

    searchStudents(requestPayload, controller.signal)
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

  React.useEffect(() => {
    listSavedSearches()
      .then(setSavedSearches)
      .catch((err: Error) => setError(err.message));
  }, []);

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
      await reindexStudents();
      setPage(1);
      setQuery((value) => value);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Reindex failed');
    } finally {
      setReindexing(false);
    }
  }

  async function saveCurrentSearch() {
    setSavingSearch(true);
    setError(null);
    try {
      const savedSearch = await saveSearch({
        name: saveName,
        query,
        filters,
        sort: requestPayload.sort,
        pageSize,
      });
      setSavedSearches((current) => [savedSearch, ...current]);
      setSaveName('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Save search failed');
    } finally {
      setSavingSearch(false);
    }
  }

  function applySavedSearch(savedSearch: SavedSearch) {
    setQuery(savedSearch.query);
    setFilters(savedSearch.filters);
    setPage(1);
  }

  async function removeSavedSearch(id: string) {
    setError(null);
    try {
      await deleteSavedSearch(id);
      setSavedSearches((current) => current.filter((savedSearch) => savedSearch.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Delete saved search failed');
    }
  }

  return (
    <main className="app-shell">
      <TopBar debugMode={debugMode} reindexing={reindexing} onDebugModeChange={setDebugMode} onReindex={reindex} />
      <SearchBox query={query} onChange={updateQuery} />
      <SelectedFilters filters={filters} response={response} onClear={clearFilter} />

      <section className="workspace">
        <aside className="filters-panel">
          <SavedSearchesPanel
            savedSearches={savedSearches}
            saveName={saveName}
            saving={savingSearch}
            onSaveNameChange={setSaveName}
            onSave={saveCurrentSearch}
            onApply={applySavedSearch}
            onDelete={removeSavedSearch}
          />
          <h2>Filters</h2>
          {response &&
            Object.entries(response.facets).map(([facetId, facet]) => (
              <FacetGroup key={facetId} facetId={facetId} facet={facet} onToggle={toggleFilter} />
            ))}
        </aside>

        <ResultsPanel
          response={response}
          selectedResult={selectedResult}
          query={query}
          loading={loading}
          error={error}
          page={page}
          pageCount={pageCount}
          onSelect={setSelectedId}
          onPageChange={setPage}
        />

        <aside className="detail-panel">
          <h2>Student detail</h2>
          {selectedResult ? <StudentDetail result={selectedResult} /> : <div className="state-message">Select a student.</div>}
        </aside>
      </section>

      {debugMode && <DebugPanel error={error} loading={loading} request={requestPayload} response={response} />}
    </main>
  );
}

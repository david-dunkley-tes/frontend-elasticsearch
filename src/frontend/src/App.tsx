import React from 'react';
import { Bug, Database } from 'lucide-react';
import { deleteSavedSearch, getSafeguardingAvailability, getVersionInfo, listSavedSearches, reindexStudents, saveSearch, searchStudents } from './api/studentSearchApi';
import { useActiveUser } from './auth/ActiveUserContext';
import { hasDslRole, USER_PRESETS, type UserPresetId } from './auth/userPresets';
import { AskPanel } from './components/AskPanel';
import { DebugPanel } from './components/DebugPanel';
import { DemoBanner } from './components/DemoBanner';
import { FacetGroup } from './components/FacetGroup';
import { ResultsPanel } from './components/ResultsPanel';
import { SearchBox } from './components/SearchBox';
import { SavedSearchesPanel } from './components/SavedSearchesPanel';
import { SelectedFilters } from './components/SelectedFilters';
import { StudentDetail } from './components/StudentDetail';
import { TopBar } from './components/TopBar';
import { citedStudentIds } from './safeguarding';
import type { Facet, Filters, SafeguardingAnswer, SafeguardingSource, SavedSearch, SearchResponse } from './types';

const reservedSearchParams = new Set(['q', 'page', 'sId']);

type UrlSearchState = {
  query: string;
  filters: Filters;
  studentIds: string[];
  page: number;
};

export function App() {
  const { presetId, preset, setPresetId } = useActiveUser();
  const canUseSafeguarding = hasDslRole(preset.token);
  const initialSearchState = React.useMemo(() => readSearchStateFromUrl(), []);
  const [query, setQuery] = React.useState(initialSearchState.query);
  const [filters, setFilters] = React.useState<Filters>(initialSearchState.filters);
  const [page, setPage] = React.useState(initialSearchState.page);
  const [debugMode, setDebugMode] = React.useState(false);
  const [response, setResponse] = React.useState<SearchResponse | null>(null);
  const [selectedId, setSelectedId] = React.useState<string | null>(null);
  const [savedSearches, setSavedSearches] = React.useState<SavedSearch[]>([]);
  const [saveName, setSaveName] = React.useState('');
  const [loading, setLoading] = React.useState(false);
  const [savingSearch, setSavingSearch] = React.useState(false);
  const [reindexing, setReindexing] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [availability, setAvailability] = React.useState<{ available: boolean; reason: string | null }>({ available: false, reason: null });
  const [safeguardingAnswer, setSafeguardingAnswer] = React.useState<SafeguardingAnswer | null>(null);
  // Exact student-id constraint (ES `terms` on student.id). Populated by a `?sId=` deep link or by the
  // safeguarding Ask auto-apply; ANDed with the viewing scope, so an unauthorised id just yields 0 results.
  const [studentIds, setStudentIds] = React.useState<string[]>(initialSearchState.studentIds);
  const pageSize = 10;

  const requestPayload = React.useMemo(
    () => ({ query, filters, studentIds, sort: query.trim() ? 'relevance' : 'studentName', page, pageSize, debugMode }),
    [query, filters, studentIds, page, debugMode],
  );

  React.useEffect(() => {
    writeSearchStateToUrl({ query, filters, studentIds, page });
  }, [query, filters, studentIds, page]);

  React.useEffect(() => {
    function applyUrlState() {
      const next = readSearchStateFromUrl();
      setQuery(next.query);
      setFilters(next.filters);
      setStudentIds(next.studentIds);
      setPage(next.page);
    }

    window.addEventListener('popstate', applyUrlState);
    return () => window.removeEventListener('popstate', applyUrlState);
  }, []);

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
  }, [requestPayload, presetId]);

  React.useEffect(() => {
    setSavedSearches([]);
    setSafeguardingAnswer(null);

    listSavedSearches()
      .then(setSavedSearches)
      .catch((err: Error) => setError(err.message));

    getSafeguardingAvailability()
      .then((next) => setAvailability({ available: next.available, reason: next.reason ?? null }))
      .catch(() => setAvailability({ available: false, reason: 'Safeguarding availability check failed' }));
  }, [presetId]);

  // When a safeguarding answer arrives, narrow the results to exactly the cited students:
  // clear the free-text query and facet filters so the list shows that set unambiguously.
  // (We don't clear on a null answer — that would wipe a `?sId=` deep link on load;
  // preset changes clear the constraint explicitly in changePreset.)
  React.useEffect(() => {
    if (!safeguardingAnswer) {
      return;
    }

    setQuery('');
    setFilters({});
    setPage(1);
    setStudentIds(citedStudentIds(safeguardingAnswer));
  }, [safeguardingAnswer]);

  React.useEffect(() => {
    let active = true;

    getVersionInfo()
      .then((versionInfo) => {
        if (active) {
          document.title = `Student Search POC v${versionInfo.version}`;
        }
      })
      .catch(() => {
        if (active) {
          document.title = 'Student Search POC';
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const selectedResult = response?.results.find((result) => result.id === selectedId) ?? response?.results[0] ?? null;
  const pageCount = response ? Math.max(1, Math.ceil(response.total / pageSize)) : 1;
  const visibleFacets = response
    ? Object.entries(response.facets)
        .filter(([facetId, facet]) => {
          if (facet.options.length <= 1) {
            return false;
          }
          // Class–teacher is only useful once the result set is narrow; hide it until ≤5 remain.
          if (facetId === 'classTeacher' && facet.options.length > 5) {
            return false;
          }
          return true;
        })
        .map(([facetId, facet]) => [facetId, sortFacetForDisplay(facetId, facet)] as const)
    : [];

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

  function drillDownFilter(facetId: string, value: string) {
    setFilters((current) => ({ ...current, [facetId]: [value] }));
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

  function changePreset(next: UserPresetId) {
    if (next === presetId) {
      return;
    }
    // Switching the "logged in" user is a demo device: reset to that user's
    // default search — clear the query, filters and the safeguarding constraint.
    // (The [presetId] effect clears the safeguarding answer, which in turn clears
    // safeguardingStudentIds; the keyed AskPanel below resets the Ask box itself.)
    setPresetId(next);
    setQuery('');
    setFilters({});
    setPage(1);
    setSelectedId(null);
    setResponse(null);
    setStudentIds([]);
  }

  function handleSourceClick(source: SafeguardingSource) {
    // The cited students are already loaded as the result set, so just focus this one.
    setSelectedId(source.studentId);
  }

  function clearStudentId(studentId: string) {
    setStudentIds((current) => current.filter((id) => id !== studentId));
  }

  return (
    <main className="app-shell">
      <DemoBanner />
      <TopBar
        presets={USER_PRESETS}
        activePresetId={presetId}
        onPresetChange={changePreset}
      />
      <SearchBox query={query} onChange={updateQuery} />
      <SelectedFilters
        filters={filters}
        response={response}
        onClear={clearFilter}
        studentIds={studentIds}
        onClearStudentId={clearStudentId}
      />
      {canUseSafeguarding && (
        <AskPanel
          key={presetId}
          enabled={availability.available}
          disabledReason={availability.reason}
          debugMode={debugMode}
          onAnswerChange={setSafeguardingAnswer}
          onSourceClick={handleSourceClick}
        />
      )}

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
          {visibleFacets.length > 0 && <h2>Filters</h2>}
          {visibleFacets.map(([facetId, facet]) => (
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
          onDrillDown={drillDownFilter}
        />

        <aside className="detail-panel">
          <h2>Student detail</h2>
          {selectedResult ? (
            <StudentDetail result={selectedResult} onDrillDown={drillDownFilter} />
          ) : (
            <div className="state-message">Select a student.</div>
          )}
        </aside>
      </section>

      {debugMode && <DebugPanel error={error} loading={loading} request={requestPayload} response={response} safeguardingAnswer={safeguardingAnswer} />}

      <footer className="page-footer">
        <button className="icon-button" onClick={reindex} disabled={reindexing} title="Reindex seed data">
          <Database size={18} />
          {reindexing ? 'Reindexing' : 'Reindex'}
        </button>
        <label className="debug-toggle">
          <input type="checkbox" checked={debugMode} onChange={(event) => setDebugMode(event.target.checked)} />
          <Bug size={16} />
          Debug
        </label>
      </footer>
    </main>
  );
}

function sortFacetForDisplay(facetId: string, facet: Facet): Facet {
  if (facetId !== 'yearGroup') {
    return facet;
  }

  return {
    ...facet,
    options: [...facet.options].sort((left, right) => compareYearGroupLabels(left.label, right.label)),
  };
}

function compareYearGroupLabels(left: string, right: string) {
  const leftYear = readYearNumber(left);
  const rightYear = readYearNumber(right);

  if (leftYear !== null && rightYear !== null && leftYear !== rightYear) {
    return leftYear - rightYear;
  }

  if (leftYear !== null && rightYear === null) {
    return -1;
  }

  if (leftYear === null && rightYear !== null) {
    return 1;
  }

  return left.localeCompare(right);
}

function readYearNumber(label: string) {
  const trimmed = label.trim();
  if (/^reception$/i.test(trimmed)) {
    return 0;
  }
  const match = /^Year\s+(\d+)$/i.exec(trimmed);
  return match ? Number.parseInt(match[1], 10) : null;
}

function readSearchStateFromUrl(): UrlSearchState {
  const searchParams = new URLSearchParams(window.location.search);
  const page = Number.parseInt(searchParams.get('page') ?? '1', 10);
  const filters: Filters = {};

  for (const [key] of searchParams) {
    if (reservedSearchParams.has(key) || filters[key]) {
      continue;
    }

    const values = searchParams
      .getAll(key)
      .map((value) => value.trim())
      .filter(Boolean);

    if (values.length > 0) {
      filters[key] = Array.from(new Set(values));
    }
  }

  const studentIds = Array.from(
    new Set(searchParams.getAll('sId').map((value) => value.trim()).filter(Boolean)),
  );

  return {
    query: searchParams.get('q') ?? '',
    filters,
    studentIds,
    page: Number.isFinite(page) && page > 0 ? page : 1,
  };
}

function writeSearchStateToUrl({ query, filters, studentIds, page }: UrlSearchState) {
  const searchParams = new URLSearchParams();
  const trimmedQuery = query.trim();

  if (trimmedQuery) {
    searchParams.set('q', trimmedQuery);
  }

  for (const [facetId, values] of Object.entries(filters)) {
    for (const value of values) {
      if (value.trim()) {
        searchParams.append(facetId, value);
      }
    }
  }

  for (const studentId of studentIds) {
    if (studentId.trim()) {
      searchParams.append('sId', studentId);
    }
  }

  if (page > 1) {
    searchParams.set('page', String(page));
  }

  const queryString = searchParams.toString();
  const nextUrl = `${window.location.pathname}${queryString ? `?${queryString}` : ''}${window.location.hash}`;
  const currentUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;

  if (nextUrl !== currentUrl) {
    window.history.replaceState(null, '', nextUrl);
  }
}

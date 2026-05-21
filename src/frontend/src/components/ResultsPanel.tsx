import { AlertTriangle, ChevronLeft, ChevronRight } from 'lucide-react';
import type { SearchResponse, SearchResult } from '../types';
import { Highlights } from './Highlights';

type ResultsPanelProps = {
  response: SearchResponse | null;
  selectedResult: SearchResult | null;
  query: string;
  loading: boolean;
  error: string | null;
  page: number;
  pageCount: number;
  onSelect: (id: string) => void;
  onPageChange: (page: number) => void;
};

export function ResultsPanel({
  response,
  selectedResult,
  query,
  loading,
  error,
  page,
  pageCount,
  onSelect,
  onPageChange,
}: ResultsPanelProps) {
  return (
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
            onClick={() => onSelect(result.id)}
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
        <button disabled={page <= 1} onClick={() => onPageChange(Math.max(1, page - 1))}>
          <ChevronLeft size={16} />
          Previous
        </button>
        <span>
          Page {page} of {pageCount}
        </span>
        <button disabled={page >= pageCount} onClick={() => onPageChange(Math.min(pageCount, page + 1))}>
          Next
          <ChevronRight size={16} />
        </button>
      </div>
    </section>
  );
}

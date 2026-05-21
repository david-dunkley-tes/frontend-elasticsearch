import { AlertTriangle, Building2, ChevronLeft, ChevronRight, GraduationCap, Hash, MapPin, Network } from 'lucide-react';
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
  onDrillDown: (facetId: string, value: string) => void;
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
  onDrillDown,
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
          <StudentResultCard
            key={result.id}
            result={result}
            selected={result.id === selectedResult?.id}
            onSelect={onSelect}
            onDrillDown={onDrillDown}
          />
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

function StudentResultCard({
  result,
  selected,
  onSelect,
  onDrillDown,
}: {
  result: SearchResult;
  selected: boolean;
  onSelect: (id: string) => void;
  onDrillDown: (facetId: string, value: string) => void;
}) {
  const matchedFieldCount = Object.keys(result.highlights).length;
  const matchTypes = getMatchTypes(result);

  return (
    <article
      className={`result-card ${selected ? 'selected' : ''}`}
      onClick={() => onSelect(result.id)}
      aria-label={`Student result ${result.student.fullName}`}
      aria-current={selected ? 'true' : undefined}
    >
      <div className="result-card-header">
        <div className="student-identity">
          <strong>{result.student.fullName}</strong>
          <span>
            <Hash size={13} />
            {result.student.id}
          </span>
        </div>
        <span className="year-badge">
          <GraduationCap size={14} />
          {result.student.yearGroup}
        </span>
      </div>

      {matchTypes.length > 0 && (
        <div className="match-type-row" aria-label="Matched result types">
          {matchTypes.map((matchType) => (
            <span className={`match-type ${matchType.kind}`} key={matchType.kind}>
              {matchType.label}
            </span>
          ))}
        </div>
      )}

      <div className="student-card-details">
        <div>
          <Building2 size={15} />
          <button
            className="drilldown-link"
            onClick={(event) => {
              event.stopPropagation();
              onDrillDown('school', toFacetValue(result.school.name));
            }}
            title={`Filter by ${result.school.name}`}
          >
            {result.school.name}
          </button>
        </div>
        <div>
          <MapPin size={15} />
          <span>{result.school.address}</span>
        </div>
        <div>
          <Network size={15} />
          {result.trust ? (
            <button
              className="drilldown-link"
              onClick={(event) => {
                event.stopPropagation();
                onDrillDown('trust', toFacetValue(result.trust?.name ?? ''));
              }}
              title={`Filter by ${result.trust.name}`}
            >
              {result.trust.name}
            </button>
          ) : (
            <span>No trust</span>
          )}
        </div>
      </div>

      <div className="student-card-footer">
        <span>{matchedFieldCount > 0 ? `${matchedFieldCount} matched field${matchedFieldCount === 1 ? '' : 's'}` : 'No highlighted fields'}</span>
        <span>{typeof result.score === 'number' ? `Score ${result.score.toFixed(2)}` : 'No score'}</span>
      </div>

      <Highlights highlights={result.highlights} />
    </article>
  );
}

function toFacetValue(label: string) {
  return label.trim().toLocaleLowerCase();
}

function getMatchTypes(result: SearchResult) {
  const fields = Object.keys(result.highlights);
  const matches = [
    {
      kind: 'student',
      label: 'Student match',
      matched: fields.some((field) => field.startsWith('student.') || field === 'studentId'),
    },
    {
      kind: 'school',
      label: 'School match',
      matched: fields.some((field) => field.startsWith('school.')),
    },
    {
      kind: 'trust',
      label: 'Trust match',
      matched: fields.some((field) => field.startsWith('trust.')),
    },
  ];

  return matches.filter((match) => match.matched);
}

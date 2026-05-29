import { X } from 'lucide-react';
import type { Filters, SearchResponse } from '../types';

type SelectedFiltersProps = {
  filters: Filters;
  response: SearchResponse | null;
  onClear: (facetId: string, value: string) => void;
  studentIds?: string[];
  onClearStudentId?: (studentId: string) => void;
};

export function SelectedFilters({ filters, response, onClear, studentIds = [], onClearStudentId }: SelectedFiltersProps) {
  const hasFilterChips = Object.values(filters).some((values) => values.length > 0);

  // Render nothing when there are no chips so the reserved row height + margin
  // collapse, leaving no gap between the search box and the Ask panel.
  if (!hasFilterChips && studentIds.length === 0) {
    return null;
  }

  return (
    <section className="selected-chips" aria-label="Selected filters">
      {/* One pill per student-id constraint (deep link / safeguarding match) — ORed, like a facet group. */}
      {studentIds.map((studentId) => (
        <button key={`sId:${studentId}`} className="student-id-chip" onClick={() => onClearStudentId?.(studentId)}>
          Student: {studentId}
          <X size={14} />
        </button>
      ))}
      {Object.entries(filters).flatMap(([facetId, values]) =>
        values.map((value) => {
          const option = response?.facets[facetId]?.options.find((item) => item.value === value);
          return (
            <button key={`${facetId}:${value}`} onClick={() => onClear(facetId, value)}>
              {response?.facets[facetId]?.label ?? facetId}: {option?.label ?? value}
              <X size={14} />
            </button>
          );
        }),
      )}
    </section>
  );
}

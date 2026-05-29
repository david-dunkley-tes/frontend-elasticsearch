import { X } from 'lucide-react';
import type { Filters, SearchResponse } from '../types';

type SelectedFiltersProps = {
  filters: Filters;
  response: SearchResponse | null;
  onClear: (facetId: string, value: string) => void;
  safeguardingStudentCount?: number;
  onClearSafeguarding?: () => void;
};

export function SelectedFilters({ filters, response, onClear, safeguardingStudentCount = 0, onClearSafeguarding }: SelectedFiltersProps) {
  const hasFilterChips = Object.values(filters).some((values) => values.length > 0);

  // Render nothing when there are no chips so the reserved row height + margin
  // collapse, leaving no gap between the search box and the Ask panel.
  if (!hasFilterChips && safeguardingStudentCount === 0) {
    return null;
  }

  return (
    <section className="selected-chips" aria-label="Selected filters">
      {safeguardingStudentCount > 0 && (
        <button className="safeguarding-chip" onClick={onClearSafeguarding}>
          Safeguarding match: {safeguardingStudentCount} {safeguardingStudentCount === 1 ? 'student' : 'students'}
          <X size={14} />
        </button>
      )}
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

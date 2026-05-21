import { X } from 'lucide-react';
import type { Filters, SearchResponse } from '../types';

type SelectedFiltersProps = {
  filters: Filters;
  response: SearchResponse | null;
  onClear: (facetId: string, value: string) => void;
};

export function SelectedFilters({ filters, response, onClear }: SelectedFiltersProps) {
  return (
    <section className="selected-chips" aria-label="Selected filters">
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

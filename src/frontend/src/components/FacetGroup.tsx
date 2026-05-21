import type { Facet } from '../types';

type FacetGroupProps = {
  facetId: string;
  facet: Facet;
  onToggle: (facetId: string, value: string) => void;
};

export function FacetGroup({ facetId, facet, onToggle }: FacetGroupProps) {
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

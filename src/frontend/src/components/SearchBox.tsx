import { Search, X } from 'lucide-react';

type SearchBoxProps = {
  query: string;
  onChange: (value: string) => void;
};

export function SearchBox({ query, onChange }: SearchBoxProps) {
  return (
    <section className="search-row">
      <Search size={20} />
      <input
        value={query}
        onChange={(event) => onChange(event.target.value)}
        placeholder="Search anything: student surname, ID, school, trust..."
        autoFocus
      />
      {query && (
        <button className="clear-button" onClick={() => onChange('')} title="Clear search">
          <X size={18} />
        </button>
      )}
    </section>
  );
}

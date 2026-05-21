import { Save, Trash2 } from 'lucide-react';
import type { SavedSearch } from '../types';

type SavedSearchesPanelProps = {
  savedSearches: SavedSearch[];
  saveName: string;
  saving: boolean;
  onSaveNameChange: (value: string) => void;
  onSave: () => void;
  onApply: (savedSearch: SavedSearch) => void;
  onDelete: (id: string) => void;
};

export function SavedSearchesPanel({
  savedSearches,
  saveName,
  saving,
  onSaveNameChange,
  onSave,
  onApply,
  onDelete,
}: SavedSearchesPanelProps) {
  return (
    <section className="saved-searches-panel">
      <h2>Saved searches</h2>
      <div className="saved-search-form">
        <input value={saveName} onChange={(event) => onSaveNameChange(event.target.value)} placeholder="Name this search" />
        <button className="icon-button" onClick={onSave} disabled={saving || !saveName.trim()} title="Save current search">
          <Save size={16} />
          Save
        </button>
      </div>
      <div className="saved-search-list">
        {savedSearches.length === 0 && <div className="empty-list">No saved searches yet.</div>}
        {savedSearches.map((savedSearch) => (
          <div className="saved-search-item" key={savedSearch.id}>
            <button aria-label={`Apply saved search ${savedSearch.name}`} onClick={() => onApply(savedSearch)}>
              <strong>{savedSearch.name}</strong>
              <span>{savedSearch.query || 'Unfiltered search'}</span>
            </button>
            <button
              aria-label={`Delete saved search ${savedSearch.name}`}
              className="delete-saved-search"
              onClick={() => onDelete(savedSearch.id)}
              title={`Delete ${savedSearch.name}`}
            >
              <Trash2 size={15} />
            </button>
          </div>
        ))}
      </div>
    </section>
  );
}

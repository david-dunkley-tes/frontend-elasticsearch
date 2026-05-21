import { Bug, Database } from 'lucide-react';

type TopBarProps = {
  debugMode: boolean;
  reindexing: boolean;
  onDebugModeChange: (enabled: boolean) => void;
  onReindex: () => void;
};

export function TopBar({ debugMode, reindexing, onDebugModeChange, onReindex }: TopBarProps) {
  return (
    <header className="topbar">
      <div>
        <h1>Student Search</h1>
        <p>Search students by name, ID, school, trust, year group, or address.</p>
      </div>
      <div className="topbar-actions">
        <button className="icon-button" onClick={onReindex} disabled={reindexing} title="Reindex seed data">
          <Database size={18} />
          {reindexing ? 'Reindexing' : 'Reindex'}
        </button>
        <label className="debug-toggle">
          <input type="checkbox" checked={debugMode} onChange={(event) => onDebugModeChange(event.target.checked)} />
          <Bug size={16} />
          Debug
        </label>
      </div>
    </header>
  );
}

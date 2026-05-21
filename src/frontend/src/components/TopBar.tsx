import { Bug, Database } from 'lucide-react';
import type { CurrentUser } from '../types';

type TopBarProps = {
  debugMode: boolean;
  reindexing: boolean;
  currentUser: CurrentUser | null;
  onDebugModeChange: (enabled: boolean) => void;
  onReindex: () => void;
};

export function TopBar({ debugMode, reindexing, currentUser, onDebugModeChange, onReindex }: TopBarProps) {
  return (
    <header className="topbar">
      <div>
        <h1>Student Search</h1>
        <p>Search students by name, ID, school, trust, year group, or address.</p>
      </div>
      <div className="topbar-actions">
        {currentUser && (
          <span className="user-pill" title={formatScopes(currentUser)}>
            {currentUser.name ?? currentUser.sub}
          </span>
        )}
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

function formatScopes(currentUser: CurrentUser) {
  return currentUser.scopes
    .map((scope) => {
      switch (scope.type.toLowerCase()) {
        case 'global':
          return 'Global access';
        case 'school':
          return `School: ${scope.schoolId ?? 'unknown'}`;
        case 'trust':
          return `Trust: ${scope.trustId ?? 'unknown'}`;
        case 'schoolgroup':
          return `School group: ${scope.schoolGroupId ?? (scope.schoolIds ?? []).join(', ')}`;
        default:
          return `${scope.type} scope`;
      }
    })
    .join('\n');
}

import type { CurrentUser } from '../types';

type TopBarProps = {
  currentUser: CurrentUser | null;
};

export function TopBar({ currentUser }: TopBarProps) {
  return (
    <header className="topbar">
      <div>
        <h1>Student Search</h1>
        <p>Search students by name, ID, school, trust, year group, or address.</p>
      </div>
      <div className="topbar-user">
        {currentUser && (
          <span className="user-pill" title={formatScopes(currentUser)}>
            {currentUser.name ?? currentUser.sub}
          </span>
        )}
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

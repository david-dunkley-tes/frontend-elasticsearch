import type { UserPreset, UserPresetId } from '../auth/userPresets';

type TopBarProps = {
  presets: UserPreset[];
  activePresetId: UserPresetId;
  onPresetChange: (id: UserPresetId) => void;
};

export function TopBar({ presets, activePresetId, onPresetChange }: TopBarProps) {
  const activePreset = presets.find((preset) => preset.id === activePresetId);

  return (
    <header className="topbar">
      <div>
        <h1>Student Search</h1>
        <p>Search students by name, ID, school, trust, year group, or address.</p>
      </div>
      <div className="topbar-user">
        <label className="user-switcher">
          <span className="user-switcher-label">Logged in as</span>
          <select
            className="user-switcher-select"
            value={activePresetId}
            onChange={(event) => onPresetChange(event.target.value as UserPresetId)}
            aria-label="Active demo user"
          >
            {presets.map((preset) => (
              <option key={preset.id} value={preset.id}>
                {preset.label}
              </option>
            ))}
          </select>
        </label>
        {activePreset && <p className="user-switcher-description">{activePreset.description}</p>}
      </div>
    </header>
  );
}

import React from 'react';
import { setActiveDevToken } from '../api/studentSearchApi';
import { DEFAULT_PRESET_ID, findPreset, USER_PRESETS, type UserPreset, type UserPresetId } from './userPresets';

const STORAGE_KEY = 'demoActiveUser';

type ActiveUserContextValue = {
  presetId: UserPresetId;
  preset: UserPreset;
  setPresetId: (id: UserPresetId) => void;
};

const ActiveUserContext = React.createContext<ActiveUserContextValue | null>(null);

export function ActiveUserProvider({ children }: { children: React.ReactNode }) {
  const [presetId, setPresetIdState] = React.useState<UserPresetId>(() => {
    const stored = readStoredPresetId();
    const initial = stored ?? DEFAULT_PRESET_ID;
    setActiveDevToken(findPreset(initial)!.token);
    return initial;
  });

  const setPresetId = React.useCallback((next: UserPresetId) => {
    const preset = findPreset(next);
    if (!preset) {
      return;
    }
    setActiveDevToken(preset.token);
    try {
      window.sessionStorage.setItem(STORAGE_KEY, next);
    } catch {
      // sessionStorage can throw in private browsing — ignore.
    }
    setPresetIdState(next);
  }, []);

  const value = React.useMemo<ActiveUserContextValue>(
    () => ({ presetId, preset: findPreset(presetId)!, setPresetId }),
    [presetId, setPresetId],
  );

  return <ActiveUserContext.Provider value={value}>{children}</ActiveUserContext.Provider>;
}

export function useActiveUser() {
  const context = React.useContext(ActiveUserContext);
  if (!context) {
    throw new Error('useActiveUser must be used inside <ActiveUserProvider>');
  }
  return context;
}

export { USER_PRESETS };

function readStoredPresetId(): UserPresetId | null {
  try {
    const value = window.sessionStorage.getItem(STORAGE_KEY);
    return findPreset(value)?.id ?? null;
  } catch {
    return null;
  }
}

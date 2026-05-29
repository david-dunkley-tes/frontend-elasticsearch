import type { AuthorizationScope } from '../types';

export type UserPresetId = 'global' | 'trust' | 'school' | 'schoolGroup';

export type UserPresetToken = {
  sub: string;
  name: string;
  scopes: AuthorizationScope[];
};

export type UserPreset = {
  id: UserPresetId;
  label: string;
  token: UserPresetToken;
};

export const USER_PRESETS: UserPreset[] = [
  {
    id: 'global',
    label: 'System Administrator (all schools)',
    token: {
      sub: 'dev-system-administrator',
      name: 'System Administrator',
      scopes: [{ type: 'global' }],
    },
  },
  {
    id: 'trust',
    label: 'Coastal Schools Trust DSL',
    token: {
      sub: 'dev-trust-coastal-dsl',
      name: 'Coastal Schools Trust DSL',
      scopes: [{ type: 'trust', trustId: 'TRUST-COASTAL-SCHOOLS' }],
    },
  },
  {
    id: 'school',
    label: 'Kingfisher Primary DSL',
    token: {
      sub: 'dev-kingfisher-dsl',
      name: 'Kingfisher Primary DSL',
      scopes: [{ type: 'school', schoolId: 'SCH-KINGFISHER' }],
    },
  },
  {
    id: 'schoolGroup',
    label: 'Local Authority Officer (3 schools)',
    token: {
      sub: 'dev-local-authority-officer',
      name: 'Local Authority Officer',
      scopes: [
        {
          type: 'schoolGroup',
          schoolIds: ['SCH-KINGFISHER', 'SCH-OAKWOOD', 'SCH-EASTGATE'],
        },
      ],
    },
  },
];

export const DEFAULT_PRESET_ID: UserPresetId = 'school';

export function findPreset(id: string | null | undefined): UserPreset | undefined {
  return USER_PRESETS.find((preset) => preset.id === id);
}

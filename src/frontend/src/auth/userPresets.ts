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
    label: 'Global admin (all schools)',
    token: {
      sub: 'dev-global-admin',
      name: 'Global Admin',
      scopes: [{ type: 'global' }],
    },
  },
  {
    id: 'trust',
    label: 'Trust admin (Coastal Schools)',
    token: {
      sub: 'dev-trust-coastal',
      name: 'Coastal Schools Trust admin',
      scopes: [{ type: 'trust', trustId: 'TRUST-COASTAL-SCHOOLS' }],
    },
  },
  {
    id: 'school',
    label: 'School admin (Kingfisher Primary)',
    token: {
      sub: 'dev-kingfisher-academy',
      name: 'Kingfisher Primary School',
      scopes: [{ type: 'school', schoolId: 'SCH-KINGFISHER' }],
    },
  },
  {
    id: 'schoolGroup',
    label: 'School group user (3 schools)',
    token: {
      sub: 'dev-school-group',
      name: 'Cross-trust school group',
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

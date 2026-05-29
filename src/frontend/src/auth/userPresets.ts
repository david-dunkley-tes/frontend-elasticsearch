import type { AuthorizationScope } from '../types';

export type UserPresetId = 'global' | 'globalNonDsl' | 'trust' | 'school' | 'schoolDslTrustView' | 'authority';

export type UserPresetToken = {
  sub: string;
  name: string;
  scopes: AuthorizationScope[];
};

export type UserPreset = {
  id: UserPresetId;
  label: string;
  description: string;
  token: UserPresetToken;
};

export const USER_PRESETS: UserPreset[] = [
  {
    id: 'global',
    label: 'System Administrator (all schools)',
    description: 'Sees every school across all trusts, with safeguarding access everywhere.',
    token: {
      sub: 'dev-system-administrator',
      name: 'System Administrator',
      scopes: [{ type: 'global', role: ['DSL'] }],
    },
  },
  {
    id: 'globalNonDsl',
    label: 'Data Analyst (all schools)',
    description: 'Sees every school’s pupils across all trusts for reporting, but is not a DSL — no safeguarding access anywhere.',
    token: {
      sub: 'dev-data-analyst',
      name: 'Data Analyst',
      scopes: [{ type: 'global' }],
    },
  },
  {
    id: 'trust',
    label: 'Coastal Schools Trust DSL',
    description: 'Safeguarding lead for the Coastal trust — sees all its schools, with safeguarding everywhere in the trust.',
    token: {
      sub: 'dev-trust-coastal-dsl',
      name: 'Coastal Schools Trust DSL',
      scopes: [{ type: 'trust', trustId: 'TRUST-COASTAL-SCHOOLS', role: ['DSL'] }],
    },
  },
  {
    id: 'school',
    label: 'Kingfisher Primary DSL',
    description: 'Safeguarding lead for Kingfisher only — sees Kingfisher pupils and their safeguarding.',
    token: {
      sub: 'dev-kingfisher-dsl',
      name: 'Kingfisher Primary DSL',
      scopes: [{ type: 'school', schoolId: 'SCH-KINGFISHER', role: ['DSL'] }],
    },
  },
  {
    id: 'schoolDslTrustView',
    label: 'Kingfisher DSL (Coastal trust view)',
    description: 'Sees every Coastal trust pupil, but safeguarding only for Kingfisher (DSL there); no safeguarding for the other trust schools.',
    token: {
      sub: 'dev-kingfisher-dsl-trust-view',
      name: 'Kingfisher DSL (trust view)',
      scopes: [
        { type: 'school', schoolId: 'SCH-KINGFISHER', role: ['DSL'] },
        { type: 'trust', trustId: 'TRUST-COASTAL-SCHOOLS' },
      ],
    },
  },
  {
    id: 'authority',
    label: 'Local Authority Officer (3 schools)',
    description: 'Oversees three schools and sees their pupils, but is not a DSL — no safeguarding access at all.',
    token: {
      sub: 'dev-local-authority-officer',
      name: 'Local Authority Officer',
      scopes: [
        { type: 'school', schoolId: 'SCH-KINGFISHER' },
        { type: 'school', schoolId: 'SCH-OAKWOOD' },
        { type: 'school', schoolId: 'SCH-EASTGATE' },
      ],
    },
  },
];

export const DEFAULT_PRESET_ID: UserPresetId = 'school';

export function findPreset(id: string | null | undefined): UserPreset | undefined {
  return USER_PRESETS.find((preset) => preset.id === id);
}

/** Whether a token grants the DSL role anywhere — used to decide if the Ask Safeguarding feature is shown. */
export function hasDslRole(token: UserPresetToken): boolean {
  return token.scopes.some((scope) => (scope.role ?? []).some((role) => role.toUpperCase() === 'DSL'));
}

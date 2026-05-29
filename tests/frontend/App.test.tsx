import '@testing-library/jest-dom/vitest';
import React from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from '../../src/frontend/src/App';
import { ActiveUserProvider } from '../../src/frontend/src/auth/ActiveUserContext';
import type { SafeguardingAnswer, SavedSearch, SearchResponse, VersionInfo } from '../../src/frontend/src/types';

const baseResponse: SearchResponse = {
  total: 2,
  tookMs: 7,
  backendTookMs: 12,
  results: [
    {
      id: 'student-1',
      student: {
        id: 'S001',
        foreName: 'Ava',
        surname: 'Harrington',
        fullName: 'Ava Harrington',
        yearGroup: 'Year 9',
      },
      school: {
        id: 'SCH-WESTBROOK',
        name: 'Westbrook College',
        address: '1 College Road',
      },
      trust: {
        id: 'TRUST-NORTH-LEARNING',
        name: 'North Learning Trust',
      },
      classGroup: {
        name: 'Acorn',
        teacher: 'Ms Priya Patel',
      },
      highlights: {
        'student.fullName': ['Ava <mark>Harrington</mark>'],
        'school.name': ['<mark>Westbrook</mark> College'],
        'trust.name': ['North Learning <mark>Trust</mark>'],
      },
      score: null,
    },
    {
      id: 'student-2',
      student: {
        id: 'S002',
        foreName: 'Ruby',
        surname: 'Khan',
        fullName: 'Ruby Khan',
        yearGroup: 'Year 10',
      },
      school: {
        id: 'SCH-EASTFIELD',
        name: 'Eastfield School',
        address: '22 East Street',
      },
      trust: null,
      highlights: {},
      score: 1.2,
    },
  ],
  facets: {
    school: {
      label: 'School',
      type: 'multi',
      selected: [],
      options: [
        { value: 'westbrook college', label: 'Westbrook College', count: 1, selected: false },
        { value: 'eastfield school', label: 'Eastfield School', count: 1, selected: false },
      ],
    },
    yearGroup: {
      label: 'Year group',
      type: 'multi',
      selected: [],
      options: [
        { value: 'Year 9', label: 'Year 9', count: 1, selected: false },
        { value: 'Year 10', label: 'Year 10', count: 1, selected: false },
      ],
    },
  },
};

const savedSearches: SavedSearch[] = [
  {
    id: 'saved-1',
    name: 'Westbrook saved',
    query: 'West',
    filters: { school: ['westbrook college'] },
    sort: 'relevance',
    pageSize: 10,
    createdAt: '2026-05-21T12:00:00Z',
  },
];

const versionInfo: VersionInfo = {
  service: 'StudentSearch.Api',
  version: '1.0.0',
  commit: 'local',
  buildTime: 'unknown',
  environment: 'Development',
};

function installFetchMock(
  searchResponses: SearchResponse[] = [baseResponse],
  initialSavedSearches: SavedSearch[] = [],
  safeguardingAnswer: SafeguardingAnswer | null = null,
) {
  const searchQueue = [...searchResponses];
  let savedSearchQueue = [...initialSavedSearches];

  vi.mocked(fetch).mockImplementation(async (input, init) => {
    const url = String(input);
    const method = init?.method ?? 'GET';

    if (url.endsWith('/api/search')) {
      return jsonResponse(searchQueue.shift() ?? searchResponses.at(-1) ?? baseResponse);
    }

    if (url.endsWith('/api/safeguarding/availability')) {
      return jsonResponse({ available: Boolean(safeguardingAnswer), reason: null });
    }

    if (url.endsWith('/api/safeguarding') && method === 'POST') {
      return jsonResponse(safeguardingAnswer ?? { answer: '', sources: [], debug: null });
    }

    if (url.endsWith('/version')) {
      return jsonResponse(versionInfo);
    }

    if (url.endsWith('/api/saved-searches') && method === 'GET') {
      return jsonResponse(savedSearchQueue);
    }

    if (url.endsWith('/api/saved-searches') && method === 'POST') {
      const body = JSON.parse(String(init?.body));
      const savedSearch: SavedSearch = {
        id: 'saved-new',
        name: body.name,
        query: body.query,
        filters: body.filters,
        sort: body.sort,
        pageSize: body.pageSize,
        createdAt: '2026-05-21T13:00:00Z',
      };
      savedSearchQueue = [savedSearch, ...savedSearchQueue];
      return jsonResponse(savedSearch, 201);
    }

    if (url.includes('/api/saved-searches/') && method === 'DELETE') {
      savedSearchQueue = savedSearchQueue.filter((savedSearch) => !url.endsWith(savedSearch.id));
      return { ok: true, text: async () => '' } as Response;
    }

    return jsonResponse({});
  });
}

function jsonResponse(body: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
    text: async () => JSON.stringify(body),
  } as Response;
}

async function waitForInitialSearch() {
  await screen.findAllByText('Ava Harrington');
}

function renderApp() {
  return render(
    <ActiveUserProvider>
      <App />
    </ActiveUserProvider>,
  );
}

function lastAuthHeaderFor(urlSuffix: string) {
  const calls = vi.mocked(fetch).mock.calls.filter(([url]) => String(url).endsWith(urlSuffix));
  const [, init] = calls.at(-1) ?? [];
  const headers = (init?.headers ?? {}) as Record<string, string>;
  return headers.Authorization;
}

function lastSearchRequest() {
  const searchCalls = vi.mocked(fetch).mock.calls.filter(([url]) => String(url).endsWith('/api/search'));
  const [, init] = searchCalls.at(-1)!;
  return JSON.parse(String(init?.body));
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn());
  document.title = 'Student Search POC';
  window.history.replaceState(null, '', '/');
  window.sessionStorage.clear();
});

afterEach(() => {
  vi.unstubAllGlobals();
  window.history.replaceState(null, '', '/');
});

describe('App', () => {
  it('shows the API version in the page title', async () => {
    installFetchMock();

    renderApp();

    await waitFor(() => expect(document.title).toBe('Student Search POC v1.0.0'));
  });

  it('renders search results and student details returned by the API', async () => {
    installFetchMock();

    renderApp();

    await waitForInitialSearch();

    expect(screen.getByText('2 students found')).toBeInTheDocument();
    expect(screen.getAllByText('Westbrook College').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Ava Harrington' })).toBeInTheDocument();
    expect(screen.getAllByText('North Learning Trust').length).toBeGreaterThan(0);
    expect(screen.getAllByText('No score').length).toBeGreaterThan(0);
    expect(screen.getByText('Student match')).toBeInTheDocument();
    expect(screen.getByText('School match')).toBeInTheDocument();
    expect(screen.getByText('Trust match')).toBeInTheDocument();
    expect(screen.getAllByText('Acorn class · Ms Priya Patel').length).toBeGreaterThan(0);
  });

  it('sends free text search changes to the API and resets to page one', async () => {
    const user = userEvent.setup();
    installFetchMock([
      baseResponse,
      {
        ...baseResponse,
        total: 1,
        results: [baseResponse.results[0]],
      },
    ]);

    renderApp();
    await waitForInitialSearch();

    await user.type(screen.getByPlaceholderText(/search anything/i), 'West');

    await waitFor(() => expect(lastSearchRequest().query).toBe('West'));
    expect(lastSearchRequest()).toMatchObject({
      query: 'West',
      page: 1,
      pageSize: 10,
      sort: 'relevance',
      debugMode: false,
    });
  });

  it('sends selected facet filters in subsequent search requests', async () => {
    const user = userEvent.setup();
    installFetchMock([
      baseResponse,
      {
        ...baseResponse,
        facets: {
          ...baseResponse.facets,
          school: {
            ...baseResponse.facets.school,
            selected: ['westbrook college'],
            options: [
              { value: 'westbrook college', label: 'Westbrook College', count: 1, selected: true },
              { value: 'eastfield school', label: 'Eastfield School', count: 1, selected: false },
            ],
          },
        },
      },
    ]);

    renderApp();
    await waitForInitialSearch();

    const filtersPanel = screen.getByRole('heading', { name: 'Filters' }).closest('aside')!;
    const westbrookFilter = within(filtersPanel).getByText('Westbrook College').closest('label')!;
    await user.click(westbrookFilter);

    await waitFor(() => {
      expect(lastSearchRequest().filters).toEqual({ school: ['westbrook college'] });
    });
    expect(screen.getByRole('button', { name: /school: westbrook college/i })).toBeInTheDocument();
  });

  it('shows the Class facet only once the result set is small (<=5 options)', async () => {
    const classTeacherFacet = (count: number) => ({
      label: 'Class',
      type: 'multi',
      selected: [] as string[],
      options: Array.from({ length: count }, (_, i) => ({
        value: `Acorn — Teacher ${i}`,
        label: `Acorn — Teacher ${i}`,
        count: 1,
        selected: false,
      })),
    });

    installFetchMock([
      { ...baseResponse, facets: { ...baseResponse.facets, classTeacher: classTeacherFacet(8) } },
    ]);
    const view = renderApp();
    await waitForInitialSearch();

    const filtersPanel = screen.getByRole('heading', { name: 'Filters' }).closest('aside')!;
    expect(within(filtersPanel).queryByRole('heading', { name: 'Class' })).not.toBeInTheDocument();

    view.unmount();

    installFetchMock([
      { ...baseResponse, facets: { ...baseResponse.facets, classTeacher: classTeacherFacet(3) } },
    ]);
    renderApp();
    await waitForInitialSearch();

    const panel2 = screen.getByRole('heading', { name: 'Filters' }).closest('aside')!;
    expect(within(panel2).getByRole('heading', { name: 'Class' })).toBeInTheDocument();
  });

  it('hides facet groups with only one available option', async () => {
    installFetchMock([
      {
        ...baseResponse,
        facets: {
          ...baseResponse.facets,
          school: {
            ...baseResponse.facets.school,
            options: [{ value: 'westbrook college', label: 'Westbrook College', count: 2, selected: false }],
          },
        },
      },
    ]);

    renderApp();
    await waitForInitialSearch();

    const filtersPanel = screen.getByRole('heading', { name: 'Filters' }).closest('aside')!;
    expect(within(filtersPanel).queryByRole('heading', { name: 'School' })).not.toBeInTheDocument();
    expect(within(filtersPanel).getByRole('heading', { name: 'Year group' })).toBeInTheDocument();
  });

  it('sorts year group facet options by year number while preserving labels', async () => {
    installFetchMock([
      {
        ...baseResponse,
        facets: {
          ...baseResponse.facets,
          yearGroup: {
            ...baseResponse.facets.yearGroup,
            options: [
              { value: 'Year 2', label: 'Year 2', count: 1, selected: false },
              { value: 'Year 6', label: 'Year 6', count: 1, selected: false },
              { value: 'Reception', label: 'Reception', count: 1, selected: false },
              { value: 'Year 1', label: 'Year 1', count: 1, selected: false },
            ],
          },
        },
      },
    ]);

    renderApp();
    await waitForInitialSearch();

    const yearGroup = screen.getByRole('heading', { name: 'Year group' }).closest('section')!;
    const options = Array.from(yearGroup.querySelectorAll('label span')).map((option) => option.textContent);
    expect(options).toEqual(['Reception', 'Year 1', 'Year 2', 'Year 6']);
  });

  it('initializes the active search from query string parameters', async () => {
    window.history.replaceState(null, '', '/?q=West&page=2&school=westbrook+college&yearGroup=Year+9');
    installFetchMock();

    renderApp();

    await waitForInitialSearch();

    expect(lastSearchRequest()).toMatchObject({
      query: 'West',
      filters: {
        school: ['westbrook college'],
        yearGroup: ['Year 9'],
      },
      page: 2,
      sort: 'relevance',
    });
    expect(screen.getByPlaceholderText(/search anything/i)).toHaveValue('West');
  });

  it('updates the query string when search state changes', async () => {
    const user = userEvent.setup();
    installFetchMock([baseResponse, baseResponse, baseResponse]);

    renderApp();
    await waitForInitialSearch();

    await user.type(screen.getByPlaceholderText(/search anything/i), 'West');
    await waitFor(() => expect(lastSearchRequest().query).toBe('West'));

    const filtersPanel = screen.getByRole('heading', { name: 'Filters' }).closest('aside')!;
    const westbrookFilter = within(filtersPanel).getByText('Westbrook College').closest('label')!;
    await user.click(westbrookFilter);

    await waitFor(() => {
      expect(new URLSearchParams(window.location.search).get('q')).toBe('West');
      expect(new URLSearchParams(window.location.search).getAll('school')).toEqual(['westbrook college']);
    });
  });

  it('drills down by school from a result card', async () => {
    const user = userEvent.setup();
    installFetchMock([baseResponse, baseResponse]);

    renderApp();
    await waitForInitialSearch();

    const avaCard = screen.getByLabelText('Student result Ava Harrington');
    await user.click(within(avaCard).getByRole('button', { name: 'Westbrook College' }));

    await waitFor(() => {
      expect(lastSearchRequest().filters).toEqual({ school: ['westbrook college'] });
      expect(new URLSearchParams(window.location.search).getAll('school')).toEqual(['westbrook college']);
    });
  });

  it('drills down by trust from the student detail panel', async () => {
    const user = userEvent.setup();
    installFetchMock([baseResponse, baseResponse]);

    renderApp();
    await waitForInitialSearch();

    const detailPanel = screen.getByRole('heading', { name: 'Student detail' }).closest('aside')!;
    await user.click(within(detailPanel).getByRole('button', { name: 'North Learning Trust' }));

    await waitFor(() => {
      expect(lastSearchRequest().filters).toEqual({ trust: ['north learning trust'] });
      expect(new URLSearchParams(window.location.search).getAll('trust')).toEqual(['north learning trust']);
    });
  });

  it('shows request and response data when debug mode is enabled from the footer', async () => {
    const user = userEvent.setup();
    installFetchMock();

    renderApp();
    await waitForInitialSearch();

    expect(screen.queryByRole('heading', { name: 'Debug Mode' })).not.toBeInTheDocument();

    await user.click(screen.getByLabelText('Debug'));

    expect(screen.getByRole('heading', { name: 'Debug Mode' })).toBeInTheDocument();
    await waitFor(() => expect(screen.getByText(/"debugMode": true/)).toBeInTheDocument());
    expect(screen.getByText(/"total": 2/)).toBeInTheDocument();
  });

  it('saves the current search and adds it to the saved search list', async () => {
    const user = userEvent.setup();
    installFetchMock();

    renderApp();
    await waitForInitialSearch();

    await user.type(screen.getByPlaceholderText(/name this search/i), 'Westbrook current');
    await user.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => {
      expect(screen.getByText('Westbrook current')).toBeInTheDocument();
    });
    const saveCall = vi.mocked(fetch).mock.calls.find(([url, init]) => String(url).endsWith('/api/saved-searches') && init?.method === 'POST');
    expect(JSON.parse(String(saveCall?.[1]?.body))).toMatchObject({
      name: 'Westbrook current',
      filters: {},
      pageSize: 10,
    });
  });

  it('restores a saved search into the active search request', async () => {
    const user = userEvent.setup();
    installFetchMock([baseResponse, baseResponse], savedSearches);

    renderApp();
    await waitForInitialSearch();

    await user.click(screen.getByRole('button', { name: /apply saved search westbrook saved/i }));

    await waitFor(() => {
      expect(lastSearchRequest()).toMatchObject({
        query: 'West',
        filters: { school: ['westbrook college'] },
        page: 1,
      });
    });
  });

  it('deletes a saved search from the saved search list', async () => {
    const user = userEvent.setup();
    installFetchMock([baseResponse], savedSearches);

    renderApp();
    await waitForInitialSearch();

    await user.click(screen.getByRole('button', { name: /delete saved search westbrook saved/i }));

    await waitFor(() => {
      expect(screen.queryByText('Westbrook saved')).not.toBeInTheDocument();
    });
  });

  it('narrows results to the students cited by a safeguarding answer, then clears them', async () => {
    const user = userEvent.setup();
    const safeguardingAnswer: SafeguardingAnswer = {
      answer: 'Two pupils show neglect indicators: [S11209] and [S11761].',
      sources: [
        { studentId: 'S11209', fullName: 'Ava Harrington', yearGroup: 'Year 9', schoolId: 'SCH-WESTBROOK', schoolName: 'Westbrook College', trustName: null, category: 'neglect', date: '2026-05-01', narrative: 'Matted hair noted.', score: 0.9 },
        { studentId: 'S11761', fullName: 'Ruby Khan', yearGroup: 'Year 10', schoolId: 'SCH-EASTFIELD', schoolName: 'Eastfield School', trustName: null, category: 'neglect', date: '2026-05-02', narrative: 'Matted hair noted.', score: 0.8 },
      ],
      debug: null,
    };
    installFetchMock([baseResponse, baseResponse], [], safeguardingAnswer);

    renderApp();
    await waitForInitialSearch();

    await user.type(screen.getByLabelText('Safeguarding question'), 'matted hair');
    await user.click(screen.getByRole('button', { name: 'Ask' }));

    await waitFor(() => {
      expect(lastSearchRequest().studentIds).toEqual(['S11209', 'S11761']);
    });

    // Each cited student shows as its own removable pill; removing one drops just that id.
    expect(screen.getByRole('button', { name: /Student: S11209/ })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Student: S11761/ }));

    await waitFor(() => {
      expect(lastSearchRequest().studentIds).toEqual(['S11209']);
    });
  });

  it('initialises the student-id constraint from the sId deep-link parameter', async () => {
    window.history.replaceState(null, '', '/?sId=S11209&sId=S11761');
    installFetchMock();

    renderApp();
    await waitForInitialSearch();

    expect(lastSearchRequest().studentIds).toEqual(['S11209', 'S11761']);
    expect(screen.getByRole('button', { name: /Student: S11209/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Student: S11761/ })).toBeInTheDocument();
  });

  it('hides the Ask Safeguarding feature and shows a role description for a non-DSL user', async () => {
    const user = userEvent.setup();
    installFetchMock();

    renderApp();
    await waitForInitialSearch();

    // Default user (Kingfisher DSL) can see the Ask panel.
    expect(screen.getByText('Ask the safeguarding records (AI)')).toBeInTheDocument();

    await user.selectOptions(screen.getByLabelText('Active demo user'), 'authority');

    await waitFor(() => {
      expect(screen.queryByText('Ask the safeguarding records (AI)')).not.toBeInTheDocument();
    });
    // The discrete role description explains why.
    expect(screen.getByText(/is not a DSL/i)).toBeInTheDocument();
  });

  it('switches the active user when the dropdown changes and sends a new auth token', async () => {
    const user = userEvent.setup();
    installFetchMock();

    renderApp();
    await waitForInitialSearch();

    const initialAuth = lastAuthHeaderFor('/api/search');
    expect(initialAuth).toMatch(/^Bearer /);

    await user.selectOptions(screen.getByLabelText('Active demo user'), 'global');

    await waitFor(() => {
      const next = lastAuthHeaderFor('/api/search');
      expect(next).toMatch(/^Bearer /);
      expect(next).not.toBe(initialAuth);
    });
  });
});

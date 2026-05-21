import '@testing-library/jest-dom/vitest';
import React from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { App } from '../../src/frontend/src/App';
import type { SearchResponse } from '../../src/frontend/src/types';

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
        name: 'Westbrook College',
        address: '1 College Road',
      },
      trust: {
        name: 'North Learning Trust',
      },
      highlights: {
        'school.name': ['<mark>Westbrook</mark> College'],
      },
      score: 3.42,
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

function mockSearchResponse(response: SearchResponse = baseResponse) {
  vi.mocked(fetch).mockResolvedValueOnce({
    ok: true,
    json: async () => response,
  } as Response);
}

async function waitForInitialSearch() {
  await screen.findAllByText('Ava Harrington');
}

function lastSearchRequest() {
  const searchCalls = vi.mocked(fetch).mock.calls.filter(([url]) => String(url).endsWith('/api/search'));
  const [, init] = searchCalls.at(-1)!;
  return JSON.parse(String(init?.body));
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn());
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('App', () => {
  it('renders search results and student details returned by the API', async () => {
    mockSearchResponse();

    render(<App />);

    await waitForInitialSearch();

    expect(screen.getByText('2 students found')).toBeInTheDocument();
    expect(screen.getAllByText('Westbrook College').length).toBeGreaterThan(0);
    expect(screen.getByRole('heading', { name: 'Ava Harrington' })).toBeInTheDocument();
    expect(screen.getAllByText('North Learning Trust').length).toBeGreaterThan(0);
  });

  it('sends free text search changes to the API and resets to page one', async () => {
    const user = userEvent.setup();
    mockSearchResponse();
    mockSearchResponse({
      ...baseResponse,
      total: 1,
      results: [baseResponse.results[0]],
    });

    render(<App />);
    await waitForInitialSearch();

    await user.type(screen.getByPlaceholderText(/search anything/i), 'West');

    await waitFor(() => expect(lastSearchRequest().query).toBe('West'));
    expect(lastSearchRequest()).toMatchObject({
      query: 'West',
      page: 1,
      pageSize: 10,
      sort: 'relevance',
      debugMode: true,
    });
  });

  it('sends selected facet filters in subsequent search requests', async () => {
    const user = userEvent.setup();
    mockSearchResponse();
    mockSearchResponse({
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
    });

    render(<App />);
    await waitForInitialSearch();

    const filtersPanel = screen.getByRole('heading', { name: 'Filters' }).closest('aside')!;
    const westbrookFilter = within(filtersPanel).getByText('Westbrook College').closest('label')!;
    await user.click(westbrookFilter);

    await waitFor(() => {
      expect(lastSearchRequest().filters).toEqual({ school: ['westbrook college'] });
    });
    expect(screen.getByRole('button', { name: /school: westbrook college/i })).toBeInTheDocument();
  });

  it('shows request and response data when debug mode is enabled', async () => {
    mockSearchResponse();

    render(<App />);
    await waitForInitialSearch();

    expect(screen.getByRole('heading', { name: 'Debug Mode' })).toBeInTheDocument();
    expect(screen.getByText(/"debugMode": true/)).toBeInTheDocument();
    expect(screen.getByText(/"total": 2/)).toBeInTheDocument();
  });
});
